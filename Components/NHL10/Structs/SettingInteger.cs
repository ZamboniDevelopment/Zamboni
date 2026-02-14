using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct SettingInteger
{
    [TdfMember("DEF")] 
    public uint mDefault;

    [TdfMember("HLAB")] 
    public string mHelpLabel;

    [TdfMember("ID")] 
    public string mId;

    [TdfMember("LABL")] 
    public string mLabel;

    [TdfMember("LOCF")] 
    public uint mLocalizedFields;

    [TdfMember("MPVL")] 
    public SortedDictionary<uint,string> mPossibleValueMap;

    [TdfMember("TOGG")] 
    public uint mToggles;
    
}