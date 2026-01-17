using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs;

[TdfStruct]
public struct FetchSettingsGroupsResponse
{
    
    [TdfMember("LGRP")] 
    public List<SettingGroup> mSettingGroupList;

}