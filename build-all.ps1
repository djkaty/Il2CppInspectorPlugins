# Build all plugins and gather them into one directory

ls */*/*.sln | % { dotnet publish $_ -c Release 2>$null }
del -Force -Recurse plugins >$null 2>&1
mkdir plugins >$null 2>&1
ls */*/bin/Release/netcoreapp3.1/publish/*.dll | ? { $_.Name -ne "Il2CppInspector.Common.dll" -and $_.Name -ne "Bin2Object.dll" } | % {
	$target = "plugins/" + (Split-Path (Split-Path (Split-Path (Split-Path (Split-Path (Split-Path ($_)))))) -Leaf)
	mkdir $target >$null 2>&1
	copy -Force -Recurse $_ $target
}
