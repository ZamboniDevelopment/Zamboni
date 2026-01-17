using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct SettingView
{
    [TdfMember("ID")] 
    public string mID;

    [TdfMember("LVDS")] 
    public List<SettingViewData> mSettingViewDataList;
    
}