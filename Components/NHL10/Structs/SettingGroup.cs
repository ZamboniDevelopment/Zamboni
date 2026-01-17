using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct SettingGroup
{
    [TdfMember("ID")] 
    public string mID;

    [TdfMember("LSET")] 
    public List<string> mLSET;

    [TdfMember("LVWS")] 
    public List<SettingView> mViewList;
    
}