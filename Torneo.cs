﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace r2warsTorneo
{
    public class Torneo
    {
        string warriorsDirectory = "warriors";
        static List<TournamentTeam> teams = new List<TournamentTeam>();
        static List<TournamentRound> rounds = new List<TournamentRound>();
        static Dictionary<long, string> teamNames = new Dictionary<long, string>();
        static Dictionary<long, string> teamWarriors = new Dictionary<long, string>();
        static r2wars r2w = null;
        RoundRobinPairingsGenerator generator;
        List<TournamentPairing> allcombats = new List<TournamentPairing>();

        TournamentTeamScore[] actualcombatscore = { null, null };
        int ncombat = 0;
        string[] actualcombatnames = { "", "" };
        string[] actualcombatwarriors = { "", "" };

        public string fullCombatLog ="";
        public string stats ="";
        public string actualCombatLog = "";

        public bool bTournamentRun = false;
        private clsEngine.eArch tournamenArch = clsEngine.eArch.x86;

        Task tournamentTask = null;
        bool bTournamenTask = false;
        bool bCombatEnd = true;

        public Torneo()
        {
            r2w = r2warsStatic.r2w;
        }
        public void SetWarriorsDirectory(string wd) {
            this.warriorsDirectory = wd;
        }

        private void espera(int veces, int pausa = 1)
        {
            Task t = Task.Factory.StartNew(() =>
            {
                int n = veces;
                while ((n--) > 0)
                {
                    System.Threading.Thread.Sleep(pausa);
                }
            });
            t.Wait();
        }


        void drawstats()
        {
            stats= string.Format("Combat {0} / {1}", ncombat ,allcombats.Count) + Environment.NewLine;
            var standings = generator.GenerateRankings();

            foreach (var standing in standings)
            {
                string salida = string.Format("{0} {1} {2}", standing.Rank.ToString(), teamNames[standing.Team.TeamId], standing.ScoreDescription);
                stats+= salida + Environment.NewLine;
            }
            r2w.send_draw_event(r2w.json_output());
            r2w.send_draw_event("on");
    
        }

        private void RoundEnd(object sender, MyEvent e)
        {
           
            int nround = e.round + 1;
            fullCombatLog   += "    Round-" + nround.ToString() + " " + r2w.Engine.players[e.ganador].name  + " Wins Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            actualCombatLog += "    Round-" + nround.ToString() + " " + r2w.Engine.players[e.ganador].name + " Wins Cycles:" + e.ciclos.ToString() + Environment.NewLine;

            if (actualcombatscore[e.ganador].Score!=null)
                actualcombatscore[e.ganador].Score+= new HighestPointsScore(1);
            r2w.send_draw_event(r2w.json_output());

            /*Task t = Task.Factory.StartNew(() =>
            {
                int n = 10;
                while ((n--) > 0)
                {
                    System.Threading.Thread.Sleep(100);
                    Console.WriteLine("Round end Waiting ...");
                }
            });
            t.Wait();*/
        }

        private void RoundExhausted(object sender, MyEvent e)
        {
            int nround = e.round + 1;
            actualCombatLog += "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            fullCombatLog += "    Round-" + nround.ToString() + " TIMEOUT Cycles:" + e.ciclos.ToString() + Environment.NewLine;
            
        }
        private void CombatEnd(object sender, MyEvent e)
        {
            string ganador = "";
            if (r2w.victorias[0] == 2)
                ganador = r2w.Engine.players[0].name;
            if (r2w.victorias[1] == 2)
                ganador = r2w.Engine.players[1].name;
            int ciclos = r2w.totalciclos;
            actualCombatLog += "Combat Winner: " + ganador + Environment.NewLine;
            fullCombatLog   += "Combat Winner: " + ganador + Environment.NewLine;


            ncombat++;
            drawstats();
            espera(1000);
            r2w.send_draw_event("off");
            bCombatEnd = true;
        }
        void runnextcombat(bool startcombat)
        {
            if (ncombat < allcombats.Count)
            {
                int j = 0;
                foreach (var teamScore in allcombats[ncombat].TeamScores)
                {
                    actualcombatnames[j] = teamNames[teamScore.Team.TeamId];
                    actualcombatwarriors[j] = teamWarriors[teamScore.Team.TeamId];
                    actualcombatscore[j] = teamScore;
                    actualcombatscore[j].Score += new HighestPointsScore(0);
                    j++;
                }
                string tmp = string.Format("Iniciando combate {0} {1} vs {2}", ncombat + 1, actualcombatnames[0], actualcombatnames[1]);
                actualCombatLog = tmp + Environment.NewLine;
                fullCombatLog += tmp + Environment.NewLine;
                bCombatEnd = false;

                r2w.playcombat(actualcombatwarriors[0], actualcombatwarriors[1], actualcombatnames[0], actualcombatnames[1], startcombat, false, tournamenArch);
            }
            else
            {
                fullCombatLog += "Tournament end " + DateTime.Now + Environment.NewLine;
                bTournamentRun = false;
                r2w.send_draw_event(r2w.json_output());
            }
        }
        public void StopTournament()
        {
            while (bTournamenTask == true)
            {
                bTournamentRun = false;
                bCombatEnd = true;
                Thread.Sleep(100);
            }
        }

        private void dopairs(string[] selectedfiles, string strarch, string extension)
        {
            generator = new RoundRobinPairingsGenerator();
            generator.Reset();
            int n = 0;
            foreach (string s in selectedfiles)
            {
                var team = new TournamentTeam(n, 0);
                teams.Add(team);
                string tmp = Path.GetFileName(selectedfiles[n]);
                teamNames.Add(n, tmp.Substring(0, tmp.IndexOf(extension)));
                teamWarriors.Add(n, selectedfiles[n]);
                n++;
            }
            // generamos todas las rondas.
            while (true)
            {
                TournamentRound round = null;
                generator.Reset();
                generator.LoadState(teams, rounds);
                round = generator.CreateNextRound(null);
                if (round != null)
                {
                    rounds.Add(round);
                }
                else
                {
                    break;
                }
            }
            foreach (TournamentRound round in rounds)
            {
                foreach (var pairing in round.Pairings)
                {
                    allcombats.Add(pairing);
                }
            }

            string memoria = "";
            for (int x = 0; x < 1024; x++)
                memoria += "\"\"" + ",";
            memoria = memoria.Remove(memoria.Length - 1);

            string salida = "";
            salida = "Tournament arch: " + strarch + "\nTotal Warriors loaded " + selectedfiles.Count().ToString() +"\nPress 'start' button to begin tournament." ;
            string envio = "{\"player1\":{\"regs\":\"\",\"code\":\"\",\"name\":\"Player - 1\"},\"player2\":{\"regs\":\"\",\"code\":\"\",\"name\":\"Player - 2\"},\"memory\":[" + memoria + "],\"console\":\"" + salida + "\",\"status\":\"Warriors Loaded.\",\"scores\":\"\"}";
            r2w.send_draw_event(envio.Replace("\n", "\\n").Replace("\r", ""));
            r2w.totalciclos = 0;
        }
        public void LoadTournamentPlayers()
        {
            StopTournament();
            r2w.StopCombate();

            if (bTournamentRun == false)
            {
                allcombats.Clear();
                teamNames.Clear();
                teamWarriors.Clear();
                rounds.Clear();
                teams.Clear();
                ncombat = 0;
                if (r2w != null)
                {
                    r2w.Event_combatEnd -= new MyHandler1(CombatEnd);
                    r2w.Event_combatEnd += new MyHandler1(CombatEnd);

                    r2w.Event_roundEnd -= new MyHandler1(RoundEnd);
                    r2w.Event_roundEnd += new MyHandler1(RoundEnd);

                    r2w.Event_roundExhausted -= new MyHandler1(RoundExhausted);
                    r2w.Event_roundExhausted += new MyHandler1(RoundExhausted);
                }

                r2w.nRound = 0;
                r2w.victorias[0] = 0;
                r2w.victorias[1] = 0;
                r2w.bDead = false;
                fullCombatLog = "";



                string[] files = Directory.GetFiles(warriorsDirectory);
                string[] selectedfiles = null;
                string extension = "";
                string[] arm32 = files.Where(p => p.EndsWith(".arm-32.asm")).ToArray();
                string[] x8632 = files.Where(p => p.EndsWith(".x86-32.asm")).ToArray();
                string[] mips64 = files.Where(p => p.EndsWith(".mips-64.asm")).ToArray();
                string strarch = "";
                int n = 0;
                if (arm32.Count() > 0 && x8632.Count() > 0)
                {
                    Console.Write("Detected mixed archs.\nWhat do you want to run:\n  1) arm 32 bit\n  2) x86 32 bit\n\n  0) exit\n\nSelect option:");
                    r2w.answer = "";
                    r2w.send_draw_event("askarch");
                    Task t = Task.Factory.StartNew(() =>
                    {
                        while (r2w.answer == "")
                            Thread.Sleep(10);
                        if (r2w.answer == "arm")
                        {
                            selectedfiles = arm32;
                            extension = ".arm-32.asm";
                            tournamenArch = clsEngine.eArch.arm32;
                            strarch = "arm 32 bits.";
                        }
                        else if (r2w.answer == "x86")
                        {
                            selectedfiles = x8632;
                            extension = ".x86-32.asm";
                            tournamenArch = clsEngine.eArch.x86;
                            strarch = "x86 32 bits.";
                        }
                        else
                            return;
                        dopairs(selectedfiles, strarch, extension);
                    });
                    return;
                }
                else if (mips64.Count() > 0)
                {
                    selectedfiles = arm32;
                    extension = ".mips-64.asm";
                    tournamenArch = clsEngine.eArch.mips64;
                    strarch = "mips 64 bits.";
                }
                else if (arm32.Count() > 0)
                {
                    selectedfiles = arm32;
                    extension = ".arm-32.asm";
                    tournamenArch = clsEngine.eArch.arm32;
                    strarch = "arm 32 bits.";
                }
                else if (x8632.Count() > 0)
                {
                    selectedfiles = x8632;
                    extension = ".x86-32.asm";
                    tournamenArch = clsEngine.eArch.x86;
                    strarch = "x86 32 bits.";
                }
                else
                {
                    Console.WriteLine("Warriors dont found. plz copy inside this folder '.x86-32' or '.arm-32' warriors.");
                    return;
                }
                dopairs(selectedfiles, strarch, extension);
            }  
        }
        public void StopActualCombat()
        {
            r2w.StopCombate();
        }
        public void StepTournamentCombats()
        {
            if (bTournamentRun == false)
            {
                RunTournamentCombats(false);
                r2w.bInCombat = true;
            }
            else if (r2w.bThreadIni == false)
            {
                
                r2w.stepCombate();
            }
        }
        public void RunTournamentCombats(bool startcombat)
        {
           
            if (bTournamentRun == false)
            {
                fullCombatLog = "Tournament start " + DateTime.Now + Environment.NewLine;
                bTournamentRun = true;
                tournamentTask = Task.Factory.StartNew(() =>
                {
                    bool firsttime = true;
                    bTournamenTask = true;
                    System.Diagnostics.Debug.WriteLine("TournamenTask: Ini.");
                    while (bTournamentRun)
                    {
                        if (bCombatEnd == true)
                        {
                            if (firsttime && !startcombat)
                                runnextcombat(false);
                            else
                                runnextcombat(true);
                        }
                        else
                            Thread.Sleep(100);
                    }
                    bTournamenTask = false;
                    System.Diagnostics.Debug.WriteLine("TournamenTask: Fin.");
                });

               
            }
            else
                r2w.iniciaCombate();
        }
    }
}
