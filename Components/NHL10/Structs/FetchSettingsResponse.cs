using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct FetchSettingsResponse
{
    
    [TdfMember("LSIN")] 
    public List<SettingInteger> mIntegerSettingList;

    [TdfMember("LSST")] 
    public List<SettingsString> mStringSettingList;
    
}