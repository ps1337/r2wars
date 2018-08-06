VERSION=2018.1
R2W=r2wars-$(VERSION)

all:
	xbuild

dist:
	xbuild /p:Configuration=Release
	rm -rf $(R2W)
	mkdir -p $(R2W) 
	cp -rf examples bin/Release/*.exe bin/Release/*.dll $(R2W)
	cp -f RELEASE_README.md $(R2W)/README.txt
	zip -r $(R2W).zip $(R2W)
	rm -rf $(R2W)

r2w32:
	wget -c http://radare.mikelloc.com/get/2.7.0/radare2_installer-msvc_64-2.7.0.exe

clean:
	rm -rf bin obj r2wars r2wars.zip
