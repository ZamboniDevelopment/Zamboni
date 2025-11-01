using System.Collections.Generic;
using System.Threading.Tasks;
using BlazeCommon;
using Zamboni.Components.NHL10.Bases;
using Zamboni.Components.NHL10.Structs;

namespace Zamboni.Components.NHL10;

internal class OsdkSettingsComponent : OsdkSettingsComponentBase.Server
{
    public override Task<FetchSettingsResponse> FetchSettingsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchSettingsResponse
        {
            mLSIN = new List<SIN>()
            {
                {
                    new SIN
                    {
                        mDEF = 1,
                        mHLAB = "1",
                        mID = "1",
                        mLABL = "1",
                        mLOCF = 1,
                        mMPVL = new SortedDictionary<int, string>()
                        {
                            {
                                1, "1"
                            },
                            {
                                2, "2"
                            },
                        },
                        mTOGG = 1
                    }
                }
            },
            mLSST = new List<SST>
            {
                {
                    new SST
                    {
                        mDEF = "1",
                        mHLAB = "1",
                        mID = "1",
                        mLABL = "1",
                        mLOCF = 1,
                        mTOGG = 1
                    }
                }
            }
        });
    }

    public override Task<FetchSettingsGroupsResponse> FetchSettingsGroupsAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new FetchSettingsGroupsResponse()
        {
            mLGRP = new List<GRP>
            {
                {
                    new GRP
                    {
                        mID = "1",
                        mLSET = new List<string>
                        {
                            {
                                "1"
                            }
                        },
                        mLVWS = new List<VWS>
                        {
                            {
                                new VWS
                                {
                                    mID = "1",
                                    mLVDS = new List<VDS>
                                    {
                                        {
                                            new VDS
                                            {
                                                mDEFS = "1",
                                                mHLAB = "1",
                                                mID = "1",
                                                mTOGG = 1,
                                                mVAL = "1"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });
    }
}