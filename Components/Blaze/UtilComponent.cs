using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blaze2SDK.Blaze;
using Blaze2SDK.Blaze.Util;
using Blaze2SDK.Components;
using BlazeCommon;

namespace Zamboni.Components.Blaze;

public class UtilComponent : UtilComponentBase.Server
{
    public override Task<PreAuthResponse> PreAuthAsync(PreAuthRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new PreAuthResponse
        {
            mComponentIds = new List<ushort>
            {
                1,
                4,
                5,
                7,
                9,
                10,
                11,
                13,
                15,
                21,
                30722,
                12,

                2049, // NHL10 Specific Component
                69 // NHL10 Specific Component
            },
            mConfig = ConfigResponse(request.mFetchClientConfig.mConfigSection),
            mQosSettings = new QosConfigInfo
            {
                mBandwidthPingSiteInfo = new QosPingSiteInfo
                {
                    mAddress = Program.GameServerIp,
                    mPort = 17502
                },
                mNumLatencyProbes = 10,
                mPingSiteInfoByAliasMap = new SortedDictionary<string, QosPingSiteInfo>
                {
                    {
                        "qos", new QosPingSiteInfo
                        {
                            mAddress = Program.GameServerIp,
                            mPort = 17502
                        }
                    }
                },
                mServiceId = 1
            },
            mServerVersion = "Zamboni " + Program.Version
        });
    }

    public override Task<PingResponse> PingAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new PingResponse
        {
            mServerTime = (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
        });
    }

    public override Task<FetchConfigResponse> FetchClientConfigAsync(FetchClientConfigRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(ConfigResponse(request.mConfigSection));
    }

    public override Task<PostAuthResponse> PostAuthAsync(NullStruct request, BlazeRpcContext context)
    {
        return Task.FromResult(new PostAuthResponse
        {
            mTelemetryServer = GetTele(),
            mTickerServer = new GetTickerServerResponse
            {
                mAddress = Program.GameServerIp,
                mPort = 8999,
                mKey = "10," + Program.GameServerIp + ":8999,nhl-2010-ps3,10,50,50,50,50,0,0"
            }
        });
    }

    private FetchConfigResponse ConfigResponse(string cfid)
    {
        return new FetchConfigResponse
        {
            mConfig = GetConf(cfid)
        };
    }

    private SortedDictionary<string, string> GetConf(string cfid)
    {
        return cfid switch
        {
            "BlazeSDK" => new SortedDictionary<string, string>() { { "pingPeriodInMs", "15000" }, { "voipHeadsetUpdateRate", "1000" } },
            "OSDK_CORE" => new SortedDictionary<string, string>(),
            "OSDK_CLIENT" => new SortedDictionary<string, string>(),
            "OSDK_NUCLEUS" => new SortedDictionary<string, string>(),
            "OSDK_WEBOFFER" => new SortedDictionary<string, string>(),
            "OSDK_EASW" => new SortedDictionary<string, string>(),
            "NHL_CUSTOM" => new SortedDictionary<string, string>(),
            "OSDK_TICKER" => new SortedDictionary<string, string>(),
            "OSDK_SPONSORED_EVENT" => new SortedDictionary<string, string>(),
            "OSDK_ROSTER" => new SortedDictionary<string, string>(),
            _ => throw new Exception("Unknown cfid: " + cfid)
        };
    }

    private GetTelemetryServerResponse GetTele()
    {
        return new GetTelemetryServerResponse
        {
            mAddress = Program.GameServerIp,
            mIsAnonymous = false,
            mDisable =
                "AD,AF,AG,AI,AL,AM,AN,AO,AQ,AR,AS,AW,AX,AZ,BA,BB,BD,BF,BH,BI,BJ,BM,BN,BO,BR,BS,BT,BV,BW,BY,BZ,CC,CD,CF,CG,CI,CK,CL,CM,CN,CO,CR,CU,CV,CX,DJ,DM,DO,DZ,EC,EG,EH,ER,ET,FJ,FK,FM,FO,GA,GD,GE,GF,GG,GH,GI,GL,GM,GN,GP,GQ,GS,GT,GU,GW,GY,HM,HN,HT,ID,IL,IM,IN,IO,IQ,IR,IS,JE,JM,JO,KE,KG,KH,KI,KM,KN,KP,KR,KW,KY,KZ,LA,LB,LC,LI,LK,LR,LS,LY,MA,MC,MD,ME,MG,MH,ML,MM,MN,MO,MP,MQ,MR,MS,MU,MV,MW,MY,MZ,NA,NC,NE,NF,NG,NI,NP,NR,NU,OM,PA,PE,PF,PG,PH,PK,PM,PN,PS,PW,PY,QA,RE,RS,RW,SA,SB,SC,SD,SG,SH,SJ,SL,SM,SN,SO,SR,ST,SV,SY,SZ,TC,TD,TF,TG,TH,TJ,TK,TL,TM,TN,TO,TT,TV,TZ,UA,UG,UM,UY,UZ,VA,VC,VE,VG,VN,VU,WF,WS,YE,YT,ZM,ZW,ZZ",
            mFilter = "",
            mLocale = 1701729619,
            mNoToggleOk = "US,CA,MX",
            mPort = 9946,
            mSendDelay = 15000,
            mKey = "some-telemetry-key",
            mSendPercentage = 75
        };
    }

    public override Task<NullStruct> SetClientMetricsAsync(ClientMetrics request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }

    public override Task<GetTelemetryServerResponse> GetTelemetryServerAsync(GetTelemetryServerRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(GetTele());
    }

    public override Task<NullStruct> UserSettingsSaveAsync(UserSettingsSaveRequest request, BlazeRpcContext context)
    {
        return Task.FromResult(new NullStruct());
    }
}