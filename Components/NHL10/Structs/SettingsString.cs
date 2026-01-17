using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct SettingsString
{
    [TdfMember("DEF")] 
    public string mDEF;

    [TdfMember("HLAB")] 
    public string mHLAB;

    [TdfMember("ID")] 
    public string mID;

    [TdfMember("LABL")] 
    public string mLABL;

    [TdfMember("LOCF")] 
    public uint mLOCF;

    [TdfMember("TOGG")] 
    public uint mTOGG;
    
}