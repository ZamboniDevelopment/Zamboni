using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs
{
    [TdfStruct]
    public struct FetchSettingsGroupsResponse
    {
        [TdfMember("LGRP")]
        public List<GRP> mLGRP;
    }
    [TdfStruct]
    public struct GRP
    {
        [TdfMember("ID")]
        public string mID;
        
        [TdfMember("LSET")]
        public List<string> mLSET;
        
        [TdfMember("LVWS")]
        public List<VWS> mLVWS;
        
    }

    [TdfStruct]
    public struct VWS
    {
        [TdfMember("ID")]
        public string mID;
        
        [TdfMember("LVDS")]
        public List<VDS> mLVDS;
    }
    
    [TdfStruct]
    public struct VDS
    {
        [TdfMember("DEFS")]
        public string mDEFS;
        
        [TdfMember("HLAB")]
        public string mHLAB;
        
        [TdfMember("ID")]
        public string mID;
        
        [TdfMember("TOGG")]
        public uint mTOGG;
        
        [TdfMember("VAL")]
        public string mVAL;
    }
}