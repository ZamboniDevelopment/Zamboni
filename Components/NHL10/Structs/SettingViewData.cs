using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct SettingViewData
{
    [TdfMember("DEFS")] 
    public string mDefaultStr;

    [TdfMember("HLAB")] 
    public string mHelpLabel;

    [TdfMember("ID")] 
    public string mId;

    [TdfMember("TOGG")] 
    public uint mToggles;
    
    [TdfMember("VAL")] 
    public string mValueStr;
    
}