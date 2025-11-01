using System.Collections.Generic;
using Tdf;

namespace Zamboni.Components.NHL10.Structs
{
    [TdfStruct]
    public struct FetchSettingsResponse
    {
        [TdfMember("LSIN")]
        public List<SIN> mLSIN;
        
        [TdfMember("LSST")]
        public List<SST> mLSST;
    }
    
    [TdfStruct]
    public struct SIN
    {
        
        [TdfMember("DEF")]
        public int mDEF;
        
        [TdfMember("HLAB")]
        public string mHLAB;
        
        [TdfMember("ID")]
        public string mID;
        
        [TdfMember("LABL")]
        public string mLABL;
        
        [TdfMember("LOCF")]
        public uint mLOCF;
        
        [TdfMember("MPVL")]
        public SortedDictionary<int, string> mMPVL;
        
        [TdfMember("TOGG")]
        public uint mTOGG;
    }
    
    [TdfStruct]
    public struct SST
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

}