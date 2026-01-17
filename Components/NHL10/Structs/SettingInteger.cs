using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct SettingInteger
{
    [TdfMember("DEF")] 
    public uint mDEF;

    [TdfMember("HLAB")] 
    public string mHLAB;

    [TdfMember("ID")] 
    public string mID;

    [TdfMember("LABL")] 
    public string mLABL;

    [TdfMember("LOCF")] 
    public uint mLOCF;

    [TdfMember("MPVL")] 
    public SortedDictionary<uint,string> mMPVL;

    [TdfMember("TOGG")] 
    public uint mTOGG;
    
}