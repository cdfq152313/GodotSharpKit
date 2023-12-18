#!/bin/sh
# 檢查參數是否正確
if [ $# -ne 1 ]; then
    echo "請提供正確的版本號作為參數，例如：./nuget.sh 2.0.0"
    exit 1
fi

# 替換版本號
new_version="$1"
sed -i "s|<Version>.*</Version>|<Version>${new_version}</Version>|g" GodotSharpKit/GodotSharpKit.csproj
echo "版本號已更新為：${new_version}"

# 上傳 nuget
dotnet.exe pack ./GodotSharpKit/GodotSharpKit.csproj
dotnet.exe nuget push ./GodotSharpKit/bin/Release/GodotSharpKit.${new_version}.nupkg