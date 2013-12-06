using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace OCRWebApi.Models
{
    //TODO: All if's while parsing need to have else with error logged when parsing fails.
    public class VehicleRestClient : IVehicleFacade
    {

        ILogger _logger;
        public VehicleRestClient(ILogger log)
        {
            _logger = log;
        }

        public VehicleEntity CreateVehicle(VehicleEntity newVehicle)
        {            
            var resultVehicleEntity = new VehicleEntity();
            EventLogEntryType eventLogType = EventLogEntryType.Information;

            try
            {
                #region Detailer Call
                //TODO: move uri to configuration file
                var requestUri = "https://api.dev-3.cobalt.com/inventory/rest/v1.0/vehicles/detail?inventoryOwner=gmps-kindred&locale=en_us";
                dynamic detailerResponse = RestClient.PostData(requestUri, GetDetailerRequestPayload(newVehicle));
                _logger.AppendMessages("Successfully completed Detailer call before Create Vehicle.");
                #endregion Detailer call

                #region Create Vehicle Call
                if (detailerResponse.vehicles != null && detailerResponse.vehicles.GetType() == typeof(JArray)
                    && detailerResponse.vehicles[0] != null && detailerResponse.vehicles[0].vehicle != null)
                {
                    _logger.AppendMessages("Detailer found atleast one vehicle in response.");

                    JObject vehicleEntity = detailerResponse.vehicles[0].vehicle;
                    vehicleEntity.Add("stockNumber", newVehicle.StockNumber ?? string.Empty);

                    if(newVehicle.photoIds != null)
                    {
                        foreach (var id in newVehicle.photoIds)
                        {
                            if (vehicleEntity.GetValue("assets") == null)
                            {
                                vehicleEntity.Add("assets", JObject.Parse("{\"dealerPhotos\": [{\"id\":" + id + "}]}"));
                            }
                        }
                    }                    

                    if (vehicleEntity.GetValue("source") == null)
                    {
                        vehicleEntity.Add("source", "M");
                    }

                    var createVehicleRequestPayload = string.Format("{{\"criteria\":{{\"vehicleContexts\":[{{\"vehicleContext\":{{\"vehicle\":{0},\"modifiedFields\":[\"assets\",\"bodyStyle\",\"bodyType\",\"certified\",\"colors.exterior.base\",\"colors.exterior.code\",\"colors.exterior.name\",\"colors.interior.code\",\"colors.interior.name\",\"createdDate\",\"descriptions\",\"doors\",\"drivetrain\",\"engine.aspiration\",\"engine.cylinders\",\"engine.description\",\"engine.displacement\",\"engine.fuelType\",\"engine.power\",\"id\",\"inventoryOwner\",\"lastModifiedDate\",\"lotDate\",\"make.Id\",\"make.label\",\"model.Id\",\"model.label\",\"odometer\",\"oemModelCode\",\"options.dealerOptions\",\"options.factoryOptions\",\"preOwned\",\"prices.discountPrice\",\"prices.internetPrice\",\"prices.invoicePrice\",\"prices.msrp\",\"prices.retailPrice\",\"prices.vendedPrice\",\"stockNumber\",\"style.Id\",\"style.trim\",\"transmission.speeds\",\"transmission.text\",\"transmission.type\",\"unmodifiable\",\"vin\",\"warranties\",\"year\"]}}}}],\"inventoryOwner\":\"gmps-kindred\",\"useSource\": true}}}}",
    vehicleEntity);

                    _logger.AppendMessages(string.Format("Request payload for create vehicle call - {0}", createVehicleRequestPayload));

                    //TODO: move uri to configuration file
                    dynamic result = RestClient.PostData("https://api.dev-3.cobalt.com/inventory/rest/v1.0/vehicles?inventoryOwner=gmps-kindred", createVehicleRequestPayload);                    

                    if (result != null && result.result != null)
                    {
                        result = result.result;
                        if (result != null
                            && result.status != null
                            && result.status.GetType() == typeof(JArray)
                            && result.status[0].vehicle != null)
                        {
                            _logger.AppendMessages("Create Vehicle call successful");

                            resultVehicleEntity.Vin = result.status[0].vehicle.vin ?? string.Empty;
                            resultVehicleEntity.StockNumber = result.status[0].vehicle.stockNumber ?? string.Empty;
                            TryGetMake(result.status[0].vehicle, resultVehicleEntity);
                            TryGetModel(result.status[0].vehicle, resultVehicleEntity);
                            TryGetOemModelCode(result.status[0].vehicle, resultVehicleEntity);
                            TryGetTrimAndStyle(result.status[0].vehicle, resultVehicleEntity);
                            TryGetYear(result.status[0].vehicle, resultVehicleEntity);

                            #region Get Color from response
                            var refStyles = result.status[0].vehicle;
                            if (refStyles.colors != null && refStyles.colors.GetType() == typeof(JArray))
                            {
                                foreach (var iColor in refStyles.colors)
                                {
                                    var color = iColor.color;
                                    var colorRefObject = new ColorReferenceEntity();

                                    if (color.category != null && color.category == "Exterior")
                                    {
                                        resultVehicleEntity.ExternalColor = new Color
                                        {
                                            Code = color.code ?? string.Empty,
                                            //Base = color.exterior.base ?? string.Empty,
                                            Name = color.name ?? string.Empty,
                                            RgbHexCode = color.RGBHexCode ?? string.Empty
                                        };
                                    }

                                    if (color.category != null && color.category == "Interior")
                                    {
                                        resultVehicleEntity.InternalColor = new Color
                                        {
                                            Code = color.code ?? string.Empty,
                                            //Base = color.exterior.base ?? string.Empty,
                                            Name = color.name ?? string.Empty,
                                            RgbHexCode = color.RGBHexCode ?? string.Empty
                                        };
                                    }
                                }
                            }

                            #endregion Get Color from response
                        }
                    }
                    
                    if(result.error != null && result.error.message != null)
                    {
                        _logger.AppendMessages("Create Vehicle call unsuccessful");
                        throw new Exception(result.error.message);
                    }
                }

                #endregion Create Vehicle Call
            }
            catch(Exception ex)
            {
                eventLogType = EventLogEntryType.Error;
                _logger.AppendMessages(string.Format("Error - {0}.", ex));
            }
            finally
            {
                _logger.LogAppendedMessages(eventLogType);
            }
            
            return resultVehicleEntity;
        }

        public void UpdateVehicleImages()
        {
            var requestPayload = "{\"criteria\":{\"vehicleContexts\":[{\"vehicleContext\":{\"vehicle\":{\"unmodifiable\":true,\"odometer\":{\"value\":10000,\"unit\":\"miles\"},\"preOwned\":false,\"certified\":false,\"vin\":\"1G4HD57217U227885\",\"source\":\"FTP.homenet\",\"make\":{\"id\":413,\"label\":\"Buick\"},\"model\":{\"id\":4403,\"label\":\"Lucerne\"},\"oemModelCode\":\"4HD69\",\"stockNumber\":\"323232431\",\"style\":{\"trim\":\"V6 CXL\",\"id\":63413},\"year\":2007,\"bodyType\":\"CARGO VAN\",\"bodyStyle\":\"A Van\",\"doors\":4,\"engine\":{\"description\":\"33.4L\",\"cylinders\":1,\"displacement\":{\"value\":1,\"unit\":\"double\"},\"aspiration\":\"EFI\",\"fuelType\":\"Compressed Natural Gas\"},\"transmission\":{\"text\":\"2=speed Automatic\"},\"drivetrain\":\"2WD\",\"lotDate\":\"2012-08-09T07:00:00.000Z\",\"colors\":[{\"color\":{\"category\":\"Exterior\",\"name\":\"Light Quartz Metallic\",\"base\":\"Copper\",\"code\":\"67U\"}},{\"color\":{\"category\":\"Interior\",\"name\":\"Cocoa/cashmere\"}}],\"prices\":[{\"price\":{\"amount\":12000,\"type\":\"msrp\",\"currencyCode\":\"USD\"}},{\"price\":{\"amount\":12500,\"type\":\"retailPrice\",\"currencyCode\":\"USD\"}},{\"price\":{\"amount\":12300,\"type\":\"internetPrice\",\"currencyCode\":\"USD\"}},{\"price\":{\"amount\":12100,\"type\":\"discountPrice\",\"currencyCode\":\"USD\"}},{\"price\":{\"amount\":100,\"type\":\"vendedPrice\",\"currencyCode\":\"USD\"}},{\"price\":{\"amount\":12000,\"type\":\"invoicePrice\",\"currencyCode\":\"USD\"}}],\"options\":{\"factoryOptions\":[{\"optionCode\":\"-EXTRA\"},{\"optionCode\":\"-PEARL\"},{\"optionCode\":\"-RET1SZ\"},{\"optionCode\":\"-ZY1\"},{\"optionCode\":\"1XL\"},{\"optionCode\":\"A45\"},{\"optionCode\":\"A51\"},{\"optionCode\":\"AE8\"},{\"optionCode\":\"AN3\"},{\"optionCode\":\"AP8\"},{\"optionCode\":\"CF5\"},{\"optionCode\":\"FE9\"},{\"optionCode\":\"K05\"},{\"optionCode\":\"KB6\"},{\"optionCode\":\"L26\"},{\"optionCode\":\"MX0\"},{\"optionCode\":\"N75\"},{\"optionCode\":\"NB8\"},{\"optionCode\":\"NC7\"},{\"optionCode\":\"NE1\"},{\"optionCode\":\"PA2\"},{\"optionCode\":\"PCI\"},{\"optionCode\":\"PCJ\"},{\"optionCode\":\"PCK\"},{\"optionCode\":\"PCX\"},{\"optionCode\":\"R6J\"},{\"optionCode\":\"R6M\"},{\"optionCode\":\"R8Q\"},{\"optionCode\":\"U2K\"},{\"optionCode\":\"U3U\"},{\"optionCode\":\"UD7\"},{\"optionCode\":\"UQA\"},{\"optionCode\":\"US8\"},{\"optionCode\":\"US9\"},{\"optionCode\":\"V22\"},{\"optionCode\":\"VK3\"},{\"optionCode\":\"YF5\"},{\"optionCode\":\"__2\"},{\"optionCode\":\"__3\"}],\"dealerOptions\":[{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"cvncxnhchfxhghfssdhadded by Test\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"SUNROOF  POWER  TILT-SLIDING\"},{\"description\":\"Evergreen package\"},{\"description\":\"Front Wheel Drive\"},{\"description\":\"Air Suspension\"},{\"description\":\"Tires - Front Performance\"},{\"description\":\"Tires - Rear Performance\"},{\"description\":\"Conventional Spare Tire\"},{\"description\":\"Aluminum Wheels\"},{\"description\":\"Power Steering\"},{\"description\":\"ABS\"},{\"description\":\"4-Wheel Disc Brakes\"},{\"description\":\"Daytime Running Lights\"},{\"description\":\"Automatic Headlights\"},{\"description\":\"Heated Mirrors\"},{\"description\":\"Power Mirror(s)\"},{\"description\":\"Intermittent Wipers\"},{\"description\":\"Variable Speed Intermittent Wipers\"},{\"description\":\"Rain Sensing Wipers\"},{\"description\":\"Multi-Zone A/C\"},{\"description\":\"Auto-Dimming Rearview Mirror\"},{\"description\":\"Driver Vanity Mirror\"},{\"description\":\"Passenger Vanity Mirror\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"Passenger Illuminated Visor Mirror\"},{\"description\":\"Leather Seats\"},{\"description\":\"Bucket Seats\"},{\"description\":\"Power Driver Seat\"},{\"description\":\"Power Passenger Seat\"},{\"description\":\"Floor Mats\"},{\"description\":\"Adjustable Steering Wheel\"},{\"description\":\"Cruise Control\"},{\"description\":\"Steering Wheel Audio Controls\"},{\"description\":\"Leather Steering Wheel\"},{\"description\":\"Woodgrain Interior Trim\"},{\"description\":\"Tire Pressure Monitor\"},{\"description\":\"Power Windows\"},{\"description\":\"Power Door Locks\"},{\"description\":\"Keyless Entry\"},{\"description\":\"Telematics\"},{\"description\":\"Navigation from Telematics\"},{\"description\":\"xbgj2epl\"},{\"description\":\"0s9\"},{\"description\":\"68.*EcsHifV8crG'sdEJpnLoXkf2BA&*.IoPi\"},{\"description\":\"xIRYOw4VfnjQgdnZj.XkRUw9FiLgp*DqmSfvr!.O3CEs\"},{\"description\":\"kUqgSZPsy2dLiT6ld5Ljs48ys7SAJ0PQDqV7UA_PnyoqCjJRL2f_sIg0lsokGPi_N6Irxy#dnwwu'.vIXAoga.XuJuc2MmI.PQd'bSTNqCjwMmkNwf6G1Cx*INhDWJ!AnYeZuZ6Fgap.o1iXJ8560CQNTI78!Qqh#iIoH5S;T1ZgMYfg44todEalH#Gmsx*!kIGFStPePoMyc!Bu2gEGWCD7IXh;Z6eWmpvDS\"},{\"description\":\"7!OP\"},{\"description\":\"prbI;9L4Lt8NiE\"},{\"description\":\"xvM.ZiKqLm*xciS&rG2O'6OKHNC$2YbNy\"},{\"description\":\"c$Y;qqeA9VB#9bt0;HZqD#hB#Lg.7nL1yDGgOVQ$9'wsI'e0.Ci*oI;KlWFfJG$cKa$6'NLUqm07'ptv60z5E;$q7vvsQ#33CTh7CSk5q7dBhI'w3JSjGH.*Icz\"},{\"description\":\"STEZ3x5VH5tO'**;s!IZZz9HSaXubNZ0TLN_3Y.kcUoyPDN$&Bb_&aUSkxGj\"},{\"description\":\"#auF&u40wN*sYcFK#rP6k1\"},{\"description\":\"#im\"},{\"description\":\"qun6RpKLauscIjL;oGePxOISFrWpJA2WgV&6IhWDN#5Q.OGd\"},{\"description\":\"kl4D_cbm*00$VBa#;wnPf7GjbB5jraql\"},{\"description\":\"PYOxSI\"},{\"description\":\"r\"},{\"description\":\"&0w!s9leCSS7tWea2'Qsuu9*tFd5*XnNYFtgnKJwyN..$MVB9Wq&3zf\"},{\"description\":\"q2_mmZKf5EJo3ynR6TtgWrChjbwR7UkY0Uq4&XJ\"},{\"description\":\"DsM7C7.!8.GJFyXyZqCL2sSLUk8II3eIKUd3f*eWC6joNbT1xrEcUyRLaBNSz!2FtxzkVtTlMgIwJ3x.!HLRYCoLD$id06X$DduPb8FD_T!*pUCRD*hrYLhrJnhjL'4g2;s58ixCBH\"},{\"description\":\"E#TWi9#\"},{\"description\":\"me5NsvCFohWsBpkLT2;iV_fqR#cZq#yTaw.Bx3z95cIUeP_PhHDZtiYOz*v1TcOWjCSFQiOp3G\"},{\"description\":\"Engine Immobilizer\"},{\"description\":\"Climate Control\"},{\"description\":\"Rear Defrost\"},{\"description\":\"This is new Dealer Option added by Test\"},{\"description\":\"xU4&igiDulHGnRpfw$y;x711K_o&_rwN1DN!E6\"},{\"description\":\"gL_WyMiKA!!0JMte!sD3LsP6CoufjDZb9tR5ZK3O16nCG!mA7Zd.BC8ovLTJRC0wLpqQv7ZZ8oCeEM0#CuS40h\"},{\"description\":\"7529OqUxYFqqQa;EzzXJNAjjiliHXkw;4Q3l$yK9Ord\"},{\"description\":\"NWJr5pS4D_66b\"},{\"description\":\"g3IN\"},{\"description\":\"&JU6ARl$BpouWWK;6bqb7$$&mjwJtRh1ARK!tqI\"},{\"description\":\"XQ'OdJK#CUhR#LA;h\"},{\"description\":\"QVc\"},{\"description\":\"kH7tEM\"},{\"description\":\"c;&RBKWwYJal_qwzJ9FJtLRrYxe57hjFxrZeNA9Bea39;9EioIX4zCg#ZcsE6q9;0*ng!SlJzE5TihUotv_Z4YFvLoZ#H5*nKmJCbXR'WO5u_7i_2DK#TKLC8s'csUB&!.Iv.SN'dBDQtLXdFhJDUD&AbH&ii'dBMr4zYnzKq'nSHvIX'r;LLxT7EIU#f4mHYBDBgYZSiyMePVtfyPNrz#qO_xuQ$846igtFrH;Q#X2Ml*RjP*$9QvImLo3j4JfT*gw92nnr.xZbId3DfLByvvPn.GuCdmm2.J.E.i11gbqT'6d750DSHhY.!IrVskNlKwDzFZ5xk\"},{\"description\":\"RTEvRL37_Xj&Uqkz&9qF8v7FI\"},{\"description\":\"2e2GuG88!E*'qfs5NqC_bMOFlvGeKVmzn.jfE#2cEm0g5y.KNDi_C#xQ9WUzzkXOS'iSwM1b80$pl7amWw!H$OwVa$PhR2dU8L8pQ0OAbGIcyI4Bc7cDTQcD.6_3x\"},{\"description\":\"4FcS0\"},{\"description\":\"zk;wh955GUX$IOFjgaAS95RkikDCcDzfikBrrgVkkQwXJFVi*bZCSHK*&mNx63jDV_kXMNsAYQ8S8tad9U7Bdx7aa7YF$YX07tC#Ux;1iZeC.rF\"},{\"description\":\"L98LwiZYKJdeon7AsIl*rJ2#eGSYS5Bi2etCqM4f*e1KTr$vreJ!F#iCHw6p9AFeIvWJt4JfdGzNhRSn&W6*V!cqwrvug*P*.cCY*2C7*3nz#'Vpd\"},{\"description\":\"VM&!3_S5NC8PzIq5*EPtSLUzArd\"},{\"description\":\"MP3 Player\"},{\"description\":\"Auxiliary Audio Input\"},{\"description\":\"Rear Reading Lamps\"},{\"description\":\"Front Reading Lamps\"},{\"description\":\"Power Outlet\"},{\"description\":\"Front Head Air Bag\"},{\"description\":\"Rear Head Air Bag\"},{\"description\":\"Passenger Air Bag Sensor\"},{\"description\":\"Child Safety Locks\"}]},\"assets\":{\"dealerPhotos\":[{\"id\":\"5748977829\"},{\"id\":\"7228570968\"},{\"id\":\"5748977355\"},{\"id\":\"5748977505\"},{\"id\":\"7228570328\"},{\"id\":\"7228670388\"},{\"id\":\"5748977449\"},{\"id\":\"5748977865\"},{\"id\":\"5748977593\"},{\"id\":\"5748977629\"},{\"id\":\"5748977665\"},{\"id\":\"5748977715\"},{\"id\":\"5748977749\"},{\"id\":\"5748977799\"},{\"id\":\"7228471524\"},{\"id\":\"5748977901\"},{\"id\":\"5748977943\"},{\"id\":\"5748978021\"},{\"id\":\"5748978145\"},{\"id\":\"5748978077\"},{\"id\":\"5748978095\"},{\"id\":\"5748978223\"},{\"id\":\"5748978271\"},{\"id\":\"7228471522\"},{\"id\":\"7228471637\"},{\"id\":\"7228570608\"},{\"id\":\"7228570708\"},{\"id\":\"7228670909\"}]},\"warranties\":{\"warranties\":[{\"value\":\"uI,w5i$dj&x*1l2N54RTAu8gBV_BCMpPiNhRU*a&ldhNTNXVtT7E'47z,suKL#a;ehlNELsrjd7WMV7oyWIjfkaYgh_1a!2UDK8$1JQh0;VsxKmY5FMo3rruH77X&#QSUNTCmv7pG02I9i0'uTv*Y*qMS*4tbtNpZC_krk#!.XiD8j5YHr2U'cufWHjS!&NMM$j6K'eJh5wBmnd9JFmj!tUcIs7jASAEJ'qwAyO#NS$Jgst94UJ4h3cTSGYSIvJ85rsRwg9KrU;R0KwPyyZgJ'Xft_et,w3SJDepAc$dky&6n4sHJJTJ'2Yp*MXZC'!vrYIW5HOW5cwLCqr9f;k!cTcgfpVoYNaO7.*9W'bCuwdt6MndUo,5lAS.b1_;l0ySBhe4W2MJuh;f'jT$B_&9&P36ZwnuxFQ*y5sf831rb!!D3$CsHF*On8emYREZ'RLp#ChKq*D.QbnDeZDmtuns!Ch9gG1QYBW3Yb$1lsC$BvxLbbHyOS6b4TY8VS3kNnUPZ3eCnbBAsUq*F'oFy2BF*bzFXEG&9RZObhmRxFL&R5iJ2j3f3YmklOae4.fi*7#auctXj_HsR8oJspd&b0JL5'27GHmZZ2yjtIOgqFvmQt$X0#l*vLHNY0OZT5Qbki$p9EPkawd'5Wu$CFH'WS4OUk5&tp64oUC80dnVVBty8$iB5cWOYaZ$YY*f,crl9ThKU6MZSmUVY5fiB3HQ$SFNu!KBJIb.JI8Pk_gz!xw$ZDNs.ldQ*aSRN&PkWKPKKcLPw!x3cIg'W&nrWR2X45E8qRBR2wdZhm1dryvIoT*6uduafY8Eisff_cZf8gBf41n!LJzYVU9Kq_*NX1$D8.;pGcFP.Q,IRoB&p38'ZKj82*oAIM1GOyRlky39EkfEa1tC'BXttweXpI6$nFG6o&uT;QjrAz#'5eoBbl4Go8NRUtvb#rbydSPG7,6ZS5tt5IsLU5qBaA_0ZczcUH0IX$rM8h*BZFo3'TmLAj1GB,XdapgkakEKFk'sRgd$roc'LOkGqnb!gUGdPO$,Aj1'AdYkduuUGoOMxi#6EaumI&4kdrcCOD$I7H*8qA2vt03rFug6dF*0o*67T&GlRsGDWGfpOD'ERzthJ&fjLQYI9LTsbGOWt_eBU0*yOrSa!!IpaeXfnXaA&y&KnmOHoTLaUs7jyh*Tye.zD4Rwj89'wd7Vm3L2cKlRY8*iQ*hGi0Cp0*H2*SXZj'd_4Mw*;M_ns1a*q9fPh3n;a.uSWin4m67ikxYE.M9'jOwzt&$F$I_0iHwlRdSoZMB6trjtKa_*tSm6cvYiPhreoHAdA$muubP;N!GCj,ur#D69U9JWZ9._UX_mrFs.ocaSCQTLpWz;nzMkb#3!2!'4D.VpsTLdhVDo3N7DQy7C9CkVKmL0c1QXHPP4u3S'LWp#vY17DZU4C$uBnAfGIlBZFggNyDJmfsrNNSZl4dWWHlV'UO;ueM60V95P_O8AQ7eKahn'atb48eRT4Ltk6PI&5#rdmJOQPVFaHKu$0BTl#Br;RLk6Ud&BZLeJpKi#f4PM,_MUQ045wXB4,V5bYhw#iEVv6ldPThGHmexaz!QuTIFCJU!*eGMx8d4*WQn5dOMORu.uXlco#BS,ySmMUI#M'I,IU2XidkRFvrkPb;Qs4X665l;D4bK*dA.IV9av8tLgkMeH8;!er.hRbMc9mTXBad2vSctMvV4#MgcSG8'nK6cET07InKIO'JGHkioldYi;OPEv!32Xd9qNAXbsPlYkNPvDa*7Y,jKmdGqDV#.3ik!GSsG7Hw;rzx3yUBVvvEduYE!u7_;dMuC2eplh8AQdNSmfdrjUQc7eH;hN1Lz90tfEvAT4P&7vnAhGRkR'oV6!7XVII_ZWAhWT&ijc.YEdO$rGpbw;u4bE7O_.fLW.NLuFd7$9B'x9QjWu6OS7aj1;GRmZKLw6C6!D.l21J7'Z6LodsdM!spx4jYz6bMYp.#7kSSWalqYNNCJu3I77z5&M8FTUmwgwIfgveD5,1#eB#jQgOEC$frqMxUWMyCopK1F7WNQ0dnG7g$bep&FhuNRQo*y3gSaZdprJej4Anbprl355,_X5wGvwKp.hPn,5Mhkra#AFJFtSYZ55I&kJuP2;uuzpKckAC4eE,*kH;RWqNd.k3N,cC!!xYOxg$Lv6UtQua0m*jd$ct#2RVO5wDIx#thg4;D*4Vp;U6eKD1EsZxnEmlf4Ygz3LveAta1xfIH0DvIk_D1*yP.*Fdu,r,PeW64YieQ3AGNQq7AqcvnUZ44vWKuQghGGt3E_!jBtny*sC1e.cDwvluZrC9C.jAdSzGX6330stu70q#tu6W&clLUyEM'Bh3JmVVsvERPSdDBr7yB!;HM76A7omY2QoYK!kbF*,xqOGVzhJ2#FLF5aQcZUoDJfpy3Yt#Hls6q.Qx0LAgf3nWFjt9THVkg'oKZIrao.VfNeOm4TWOTIYfs$ta53RA!IgUkG.STZsHIHJ0'GFAkG!.RIRmORP40VPFytBYaEutQQ_l3$EYXQP3_HCbomDCK7gnsE2bEh0gM_1A#62cMqHKGl7qq09oBsjj!ksZK3U;q9RzgBK130pU0'hG;#keR#eTn.!h*D$i;#U;*8KoPI5aD93uDmj.,'YYkxMH#3n0oGdoVK#I1v8IGEQYP;ehUtJAMwm;VDXmVElwl*,sUaRdGPRghnrvfFGuxdcZYumY_cnBST$bxl6,c5tweFo8L2HQyrzIrT*0otG42Q88UQyt.6S#jfaSFXMP';C0U4jdQGkjZ9*8_0Nu4N_#.7ZM,WU!grolNHOq6Ud#c8MTLMbfKRk1y!hmF_EtmTD8aiV9MX8J!GpKVFA6b'08G7ncZ1kpQjf25e.3rL$njbrsb_#z;_SRt*1Nv,Kl$.sg16e;6;'!C&e5'uAhqlS,#79rW58SAhUKiK!Ptu$zBhO0qw_A.2vg0TOtwnUBIX9VS&c43lovSS_jTJAZo8,.m&j9w_SG,#reSsHWUM;n*z8Ib5anmuNadr9ejN8m$hVg0s#0CeFbrmDmFbn!w5TfYRB132$GdjVmdhhiuTkHp#irnc,IZypG7;2JrJw,MyyH!QJXtF;daMpm8K&hj3hfuOZGZ.rkXLuq&a0;&VOT1M5Vg'ah'PpiQTNWrCyVOmZ8RzTojwO8P8XhiD0jxJOIWM,U6Df!XtF'mwDhWmx.A1&iv5&ncPXE_RMFZz1fRdKV2ivmz.7LM'Hv.rYB!roI;By*ky2W4R1XQS0N!hnEhLsBKR78a0PZoCf1j1BBPKhuOke0!L#jDk04Iw3&#ubECB8N$z3Jfmo;4yn0$Gx8p5WEbVSnx8!V8drMW!n#i2DRwkAKJDA4wNP;f3#Rh;LRV&HgGaEBT6g72FIrUHdt7GYV8U7sy;kMQlv2cTE9zyK!6hOaVaw,sRkPPlSlMLDcb&Q;JfPACNu8vnogGn_Y,KTyBgNqBSXnv8ps#_!'4v;eXVT2w$fY*eC#US'3$YzkqG$7ywP8XBud&*bf6kt8$yTQ5X.TCGCFwmoQim4AXQB$kYxf;pxh5!RsnWBaXv6Qzohhg4w,0sLWxh6GZtcMJUa5*w36ljd1kk$w_QyMtY1$ZvuFpuU4TUHeNWMkB9o_Pk#NO!9Iu9TltIG,BX_q8SH!dniVENK39kW*3hrZ5M9jAVlFP$jesyIwcB90iODgZpVJl#QU5R4WjX91CcF$46!#cZr1R!_RIi4KAcUdW8Sa&lNZ$HYFI#WtZj65aoCvaNOAsY1Z21PvNqyqM$pJM#I;6#lQPoD76XzZ_N6OgydgV0g8l;TIzsiJF_&1$LJ*Yi9pq.TzFb3No1VGlX4,*v8&*qUz,Fb'oI2&qq.MXUDY9'VeW7;Pg!z3TvUAYTZzzoYfyhPueSWTsKEz''dL96oYIzTv'.nDuDYaR'f*Sb!8Og#JrV4i;O!&zA;z7TZG4SZol3hzqIsnv0J27KDTNF7g$kbRyfGyW4Dw0cX8*5s7Vd.,qO#XQFtZl,WBi!AKBu#\"}]},\"descriptions\":{\"description\":[{\"value\":\"xYUOBB2_qPeyHmSmqVmQ1vrnEoc.c;gR83KHfKwhz1A&&JFd#vXG#4oC66JNZjvT879dcKGfZERAvXUvjzHhTagE*HaTAi2P6gEG,2A0o_g6NoHILO3ZcNAog5I'9ne;bB&PTOV1ygsbtTrgA#HmZD$SfQZOT5rBsJlLUspxtC0wUYu7*2*xgsR.9U9DpGH&prik8f5IcJtWc'x0VraqqtUAsMcA2mHmunH3;Kyox6V;zvwtXIhx6Qcw_isVD5fpBG1o8*kLslt3_dBwQicTFK0dLLlnScl$h6's&I4iY'EaXSYthvFUi5krgdcXiwsPA1cLII2IYuv9Ff0tmsGX_!jVbdLq5Hz1coav'Nk.$xwn!UlBWj!qp'.9_195w$cRu*mQE4X,I8k,*wl1rK&c;J#GPLYNdZbzVfsv7*wr5rh1lzub;I&mzAalCfAtGJwimkYpDptXNbQe1JVQQc_'mpAVEHsh&DlVvz*AaE.gOP.Xjw3s4kc4$1wwkDlvwLjKqvj2*V'LDq0;Rd#HdAXj10j*C_Q_TckC687nMBXIO!0bA0fG#dSwozi9MP0wFys1GXJiQeuccP'rAH&csJG'4nO7fxbxb1DQ_z'dQwqni;U,4r9EbeEwrWxSJcDSCxBVLYTA$q5vSTno&'Cn7;WxWGYVCW**t_kl'z7F.8$lSJd.9If&,Pl6bEyFzxyPVqBa8BT&HxPyD!eM3gmt;AuemYK!A2Fa!W!2HDK7pM6cAm2KZmcTLF,VvXoKri$fr*8hAVEZ'YD4*SezvzLOgl95dO6kPA'H,gl5YbKDhWenBOhd,oOs;YzFFDDlpUK'ivln7SaUoH73Hb387iL0ld!UhfZV_YO6LIEG3tQ0NtD9q'x3,'YGIjkE4!FhtZd9sARz_XF&$lNsaB4MhvbBAHY2Yzbso87oQ&;qXymW!k*X;s43Gtzxdb8lcpb_bMfDYYbG4&ojJZ#Q&Al5'Rmu89L*ElO7WPbXF!zSQXz$EQnYJSRbDPC40k'oNHC.Z06udOKDB&PRRTqScn4l*$4HMQwVvLFIV1NdjtavINa!zkMHuWM0dQDnwZh7RpIp8HwqGM8iM8d1rtH2Ep1.yf5EY0h4MNeTEkCsnWAio3NO&cvDRY218!qjbs$g4D;zk4WGrP'io,k'#R#b4eNnkWk*tSQvjUXknN3J0oLCia$pfVR40qVgqiO8,hM.yay*VAhYn2V'$KEKkvp.Al*tz;tEyv9.b$DIby&f&t_y*gOfeEeds*_NGF*fGdZssctzHw9l2JEHO'YT2N0uRmgHuqC;3n.q*eFsmO.h*sVi!!y!gVXtX4aGvmFRts1*#O&9956N'.ZY#y_4r$pI!e&JDzwAJ0oc!gRVx9AUpJYeEW'xPa01CTDmb35!96DOkdokhV61#'S00oFh4zl8A6,Qc5a&D9l3GJu07kQtNyp7Ad5mXdG9a*OXE_HWatYCjpUQueT,jrUB_LQzzoPZ3Vi#v;WqduTUaneK2Os,46gjTMgt1IWmdy.7HjUX23wp.A1k6F;kw;vw'yRjq29*_7_cOQD,t*QB!rLTlv6V#$qGCcsJ##6zt1RL2e'QCxq10u5dnxxvwODF;#,$YuI2IQHI,zrG0PT*b8gHQOV**#DfXM*qFDsIp8TJmXEnGr*gu93Il$VJ.&n;N2k75ABWXLfNP;UtK77dwXkoi6F&hi9Vacw4$;1FpS2jjQsOR2zKFKaq.wezu8**b4$&aq1&G1neuBE$&rVZjYP9CGm's2L1doHZdZ*0B5EeXvw_rM6DMPkWOD#*ElbdQ6AFtepOWvYYU#MGKOefwHO8DlL,nAWPj07quqeMXRgRr6Cdq43pHvhIS$CH&P0W.I*tDVLT!#2e1x__i7i2;6Io,Y.2o;.Hq3.irJ,'TKY'$DhUv4YG6ygLzfUfdj5usVoxYof,nNJ6I&W;;X63R$bsd6jLour!e3cRPkwGfeTSoUD63dIZWCc0,vUrF&ksW.heqE1m10e$T2D23WhLm#OcgV_T!EBLt0wQ$Y'ZWWywYKHRrr8il,Gk4Fx'7HVWY$.s6nqEh2o9$RX;O01.;S0TULl60ArPAc_l#OkEB3B2l;RMh10HfdfGMY.UB3dh!,9qa$Fz'X_MRx8du.ez,k9Xw!,M;!C.gTrMH1#PV1#2F&pPqv03edIgKMtKst3FUz6yFV;&G9eSSGR3b4VbK,tCLLCu71X2x6HFGrK*YnWPzp7Du988_hvbF4mtTbFfLVUmyMXkUgWXXeCVdyAytokQ#6AbEQVdh_Dd'bFiMdOq5.Y11#PUbGlX#EV$MbgD5,'fbYgCtRlPatktH.U$ABBblfM!0&;!xK5mC.fCkN03U;c_T14&1ND4_uuvMPc0bUS98yq9tRima_0PC'RyIDJjGBOIekq'#AizYNJV#v,K5Qjz&es;yeR_L826jhkJ0MVap0rp6v6N3zYqRP'zrT;niKopkc#jK!jSvJJNnCKCRDOLJ$O&MkRG;8re#BFj4dDsQGuIF1MpJ&;fwcgR8x_ZTdb7S4blw0.*AVvD.G8k.2wCsMipVoV6Fd,&47pag0sivBeahd.19mhrg,*Fcm!IFM8Wb2.KQnDR9OHq7HH_PtwSwhmlsjyuqjdi&2zYE8Lcj91##e.*essLQPZu'nR6oI1f#YXEIi&hiT970VTOL4Qxr#$XDFjQQenaHbAN1&5wZL3wgr&U*CV4s4oAmzBDzW!D5pZeUFn_yU_fH;_L#8cO33hdnA11$2wDbZ!q#pz8Pd6.mJsP2p$!,WaCIcrUe6F;X&DhFg,b9NygxIqdHLzcp63Zf7ipyeki_qVTGKKbPqB6XuFzmNN&62gqKdC_xkdk3QIGm__wBX6y5xCl2jCk#RVOJ4I2H$d5NQDHlyMP;Q$Jwh5MbRPOhsSMutw28Po8aBlOuvz3;nOUeMS2zwgqW9dVxqMrBkg11D9uwQSfRm.tex7hb*2$8yMKX!QDiI1mHmsbdX5jdA.!rk;z35Q72Os3rY_6zPxJ4lmrElDXI,o7ZbT&tR,Xru4su9jZgzj8D8GFxev_1sYiZ5DQEE$i'aR!1ux'Be_Jp03q'O'PWCvz'b$85&Uc6Bt3Q;dDgZ'dVPtzBMap8X6$eSug19K73uco6Aay#$8w6Sd0i&7St5OJ*0eybwcRPHT0&F$B$bYwo9G6**ynBr*yg&TGVcX*pRhYzXmcB6fRGRUFTR3HyoPX$U,Al5fqYjG_ih7RhVm'LXs9SNmDSP;AB3lfemr'hpsXLcW$kMZcCaMP1Kh_.b.JMF;Ywi$u4QoW5*X'rUTGpux0R;X&FTwFRiKFGx'C,5RmmpjnmLgY2#TSHPHZ4zpLTiFc3R9teQ64cL*1m!gt$.df9sxrqQ_,;G$DkH3'cn,LEp'waf&uSv,bEZN#T_C8.mn'.#_fHLRey#a,WBQLdd1a7ry4urVElK&RBAXJHnmHduc&f&OS1rlu5NIh$MlPqNtny2.WeHDpqUx$O5f0cDdtZ.mor'_vC_Q!Bo*Wus&TnwDLP'&G74$n9z80;k4I!InTD6#Zq36k93F3*a.WC!7.E3BB0OfK75jfIkHjDNPExiXGtElwLIGzaOP,5!zcdWWD,0Uk$pSoU3aVFuHV2jyYYjFu*8Y&O$0b5T6&kB3WqReQ*t*!2dUuXyfpU0PE#eahDyql8*#00c8HFoZaunL!S6Y7*;S4mYlM;xN'DiBoMt&b!_k6A*Kd&K9;a3RsVG*BqV!Hbj_LlWXYscxwlm3ROI,67_v,#J$FZ$kOHckQxyrQnKdx6UG9dXnicDaEoxackRz0sipIBMUPQ3,XtxNRCD2BfC$DCg2zXP5'KXFd4y7#D_#T4WLqj3,hpxapQlFUkUsBxDM's!mE'ryI!S,g'#Pu'egNcP6,sMgif!V0K'yT_'Lxz$ZxMZeSEx6SXzscENL2KP2zxS;A$wWz&iNJp86&87dhECDKdVP44injKZAtjefIb6f*lim'g_uc$bvuO5WRhs'kv4z!8$kjh'G_\"}]},\"id\":1809533563},\"modifiedFields\":[\"assets\",\"unmodifiable\"]}}],\"inventoryOwner\":\"gmps-kindred\",\"useSource\":true}}";
        }

        /// <summary>
        /// Obtain vehicle record by VIN
        /// </summary>
        /// <param name="vin">The VIN</param>
        /// <returns>Vehicle object associated with the VIN</returns>
        public VehicleEntity GetYearMakeModelByVin(string vin)
        {
            EventLogEntryType eventLogType = EventLogEntryType.Information;
            try
            {                
                //TODO: pull URL from configuration file.
                var requestUri = "https://api.dev-3.cobalt.com/inventory/rest/v1.0/vehicles/detail?inventoryOwner=gmps-kindred&locale=en_us";
                string jsonMessage = "{\"vehicles\":[{\"vehicle\":{\"vin\":\"" + vin + "\"}}]}";
                _logger.AppendMessages(string.Format("GetYearMakeModelByVin request payload {0}.", jsonMessage));
                var response = GetVehicleEntityByParsingJSON(RestClient.PostData(requestUri, jsonMessage));

                return response;
            }
            catch(Exception ex)
            {
                eventLogType = EventLogEntryType.Error;
                _logger.AppendMessages(string.Format("Error in GetYearMakeModelByVin {0}.", ex.Message));
            }
            finally
            {
                _logger.LogAppendedMessages(eventLogType);
            }

            return null;
        }
        
        /// <summary>
        /// Get Vehicle Taxonomy possibilities by Year, Make and Model
        /// </summary>
        /// <param name="year">The Year</param>
        /// <param name="make">The Make </param>
        /// <param name="model">The Model</param>
        /// <returns>Taxonomy list</returns>
        public IEnumerable<VehicleEntity> GetTaxonomyRecordsByYearMakeModel(string year, string make, string model)
        {
            var eventLogType = EventLogEntryType.Information;
            try
            {
                //TODO: pull URL from configuration file.
                var requestUri = string.Format("https://api.dev-3.cobalt.com/inventory/rest/v1.0/taxonomy/search?inventoryLocale=en_us&inventoryOwner=gmps-kindred&make={1}&model={2}&year={0}", year, make, model);
                _logger.AppendMessages(string.Format("GetTaxonomyRecordsByYearMakeModel requesting API with URL {0}.",requestUri));
                var result = GetTaxonomyListByParsingJson(RestClient.GetData(requestUri));
                _logger.AppendMessages("Successfully completed GetTaxonomyRecordsByYearMakeModel.");
                return result;
            }
            catch(Exception ex)
            {
                eventLogType = EventLogEntryType.Error;
                _logger.AppendMessages(string.Format("Error in GetTaxonomyRecordsByYearMakeModel - {0}.", ex.Message));
            }
            finally
            {
                _logger.LogAppendedMessages(eventLogType);
            }
            return null;
            
        }

        public ReferenceDataEntity GetOptionsByStyleId(string styleId)
        {
            EventLogEntryType eventLogType = EventLogEntryType.Information;
            try
            {
                //TODO: pull URL from configuration file.
                var requestUri = string.Format("https://api.dev-3.cobalt.com/inventory/rest/v1.0/reference/search?inventoryLocale=en_us&inventoryOwner=gmps-kindred&loadColors=true&styleId={0}", styleId);
                _logger.AppendMessages(string.Format("GetOptionsByStyleId using URI {0}.", requestUri));
                var jsonResult = RestClient.GetData(requestUri);
                _logger.AppendMessages("Successfully got data from API - GetOptionsBySytleId.");
                var result = new ReferenceDataEntity
                {
                    Colors = GetColorReferenceEntities(jsonResult),
                    Options = GetOptionsListByParsingJson(jsonResult)
                };
                return result;
            }
            catch (Exception ex)
            {
                eventLogType = EventLogEntryType.Error;
                _logger.AppendMessages(string.Format("Error in GetOptionsByStyleId.", ex.Message));
            }
            finally
            {
                _logger.LogAppendedMessages(eventLogType);
            }

            return null;

        }

        private string GetDetailerRequestPayload(VehicleEntity newVehicle)
        {
            string colorPayload = "\"colors\":[{0}]", optionsPayload = null;

            var basicVehiclePayload = "\"vin\":\"" + (newVehicle.Vin ?? string.Empty) + "\",\"year\":" + (newVehicle.Year.ToString() ?? string.Empty) + ",\"make\":{\"id\":" + (newVehicle.MakeId ?? string.Empty) + ",\"label\":\"" + (newVehicle.Make ?? string.Empty) +
                "\"},\"model\":{\"id\":" + (newVehicle.ModelId ?? string.Empty) + ",\"label\":\"" + (newVehicle.Model ?? string.Empty) + "\"},\"style\":{\"id\":" + (newVehicle.StyleId ?? string.Empty) + ",\"label\":\"" + (newVehicle.Style ?? string.Empty) + "\",\"trim\":\"" + (newVehicle.Trim ?? string.Empty) + "\"},\"oemModelCode\":\"" + (newVehicle.OEMCode ?? string.Empty) + "\"";//}]}";

            if (newVehicle.ExternalColor != null && newVehicle.InternalColor != null)
            {
                //TODO: using name for base color too. might need to fix it.
                var twoColorsOfTheVehicle = string.Format("{{\"color\":{{\"category\":\"Exterior\",\"name\":\"{0}\",\"base\":\"{0}\",\"code\":\"{1}\"}} }},{{\"color\":{{\"code\":\"{2}\",\"name\":\"{2}\",\"category\":\"Interior\"}} }}", newVehicle.ExternalColor.Name ?? string.Empty, newVehicle.ExternalColor.Code ?? string.Empty, newVehicle.InternalColor.Code ?? string.Empty, newVehicle.InternalColor.Name ?? string.Empty);
                colorPayload = string.Format(colorPayload, twoColorsOfTheVehicle);
            }
            else
            {
                colorPayload = string.Format(colorPayload, string.Empty);
            }

            string factoryOptionsArray = string.Empty;
            if (newVehicle.Options != null)
            {
                factoryOptionsArray = string.Empty;
                foreach (var option in newVehicle.Options)
                {
                    var factoryOptionEntity = string.Format("{{\"id\":4,\"optionCode\":\"{0}\",\"description\":\"{1}\"}}", option.OptionCode, option.Description);

                    if(factoryOptionsArray == string.Empty)
                    {
                        factoryOptionsArray = factoryOptionEntity;
                    }
                    else
                    {
                        factoryOptionsArray = string.Format("{0},{1}", factoryOptionsArray, factoryOptionEntity);
                    }
                    
                }
                factoryOptionsArray = string.Format("\"factoryOptions\":[{0}]", factoryOptionsArray);
            }
            optionsPayload = string.Format("\"options\":{{{0}}}", factoryOptionsArray);

            return string.Format("{{\"vehicles\":[{{\"vehicle\":{{{0},{1},{2}}} }}]}}", basicVehiclePayload, colorPayload, optionsPayload);
        }

        //TODO: Still need to cleanup Colors. They could be repeated now.
        private IEnumerable<ColorReferenceEntity> GetColorReferenceEntities(JObject json)
        {
            var colorResults = new List<ColorReferenceEntity>();
            var responseObject = json.ToObject<dynamic>();

            if (responseObject != null)
            {
                if (responseObject.searchResult != null)
                {
                    if (responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles.GetType() == typeof(JArray))
                    {
                        foreach (var refStyles in responseObject.searchResult.referenceStyles)
                        {
                            TryGetColors(refStyles, colorResults);
                        }
                    }
                }
            }

            return colorResults;
        }

        private IEnumerable<OptionsEntity> GetOptionsListByParsingJson(JObject json)
        {
            var optionsResults = new List<OptionsEntity>();
            var responseObject = json.ToObject<dynamic>();

            if (responseObject != null)
            {
                if (responseObject.searchResult != null)
                {
                    if (responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles != null
                        && responseObject.searchResult.referenceStyles.GetType() == typeof(JArray))
                    {
                        foreach (var option in responseObject.searchResult.referenceStyles)
                        {
                            if (option.options != null
                                && option.options.factoryOptions != null
                                && option.options.factoryOptions.GetType() == typeof(JArray))
                            {
                                foreach (var factoryOption in option.options.factoryOptions)
                                {
                                    if (factoryOption.optionCode != null
                                        && factoryOption.optionCode.Value != null
                                        && factoryOption.description != null
                                        && factoryOption.description.Value != null)
                                    {
                                        optionsResults.Add(new OptionsEntity
                                        {
                                            OptionCode = factoryOption.optionCode.Value,
                                            Description = factoryOption.description.Value
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return optionsResults;
        }

        private IEnumerable<VehicleEntity> GetTaxonomyListByParsingJson(JObject json)
        {
            var taxonomyResults = new List<VehicleEntity>();
            var responseObject = json.ToObject<dynamic>();

            if (responseObject != null)
            {
                if (responseObject.searchResult != null)
                {
                    if(responseObject.searchResult.taxonomy != null 
                        && responseObject.searchResult.taxonomy.GetType() == typeof(JArray))
                    {
                        foreach (var taxonomyObject in responseObject.searchResult.taxonomy)
                        {
                            if(taxonomyObject.taxonomyRecord != null)
                            {
                                var vehicleEntity = new VehicleEntity();
                                TryGetMake(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetModel(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetYear(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetOemModelCode(taxonomyObject.taxonomyRecord, vehicleEntity);
                                TryGetTrimAndStyle(taxonomyObject.taxonomyRecord, vehicleEntity);
                                taxonomyResults.Add(vehicleEntity);
                            }
                        }
                    }
                }
            }

            return taxonomyResults;
        }

        /// <summary>
        /// Mapper for transforming incoming object to the one service returns
        /// </summary>
        /// <param name="responseJson">JSON to be parsed</param>
        /// <returns>Resultant object structure of vehicle</returns>
        private VehicleEntity GetVehicleEntityByParsingJSON(JObject responseJson)
        {
            var response = responseJson.ToObject<dynamic>();
            var returnEntity = new VehicleEntity();

            if(response !=null)
            {
                if(response.vehicles != null && response.vehicles.GetType() == typeof(JArray)
                    && response.vehicles[0].vehicle != null)
                {                    
                    TryGetMake(response.vehicles[0].vehicle, returnEntity);
                    TryGetModel(response.vehicles[0].vehicle, returnEntity);
                    TryGetYear(response.vehicles[0].vehicle, returnEntity);
                }
            }

            return returnEntity;
            
        }

        
        #region Parse individual Vehicle Objects from JSON
            private void TryGetMake(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.make != null
                            && jsonObject.make.label != null
                            && jsonObject.make.id != null
                            && jsonObject.make.label.Value != null
                            && jsonObject.make.id.Value != null)
                    {

                        vehicle.Make = jsonObject.make.label.Value.ToString();
                        vehicle.MakeId = jsonObject.make.id.Value.ToString();
                    }
                }
                catch
                {
                    //TODO: take care of logging.
                }

            }
            private void TryGetModel(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.model != null
                            && jsonObject.model.label != null
                            && jsonObject.model.id != null
                            && jsonObject.model.label.Value != null
                            && jsonObject.model.id.Value != null)
                    {

                        vehicle.Model = jsonObject.model.label.Value.ToString();
                        vehicle.ModelId = jsonObject.model.id.Value.ToString();
                    }
                }
                catch
                {
                    //TODO: Logging
                }

            }
            private void TryGetYear(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.year != null
                                && jsonObject.year.Value != null)
                    {
                        int year;
                        Int32.TryParse(jsonObject.year.Value.ToString(), out year);
                        vehicle.Year = year;
                    }
                }
                catch
                {
                    //TODO: Logging
                }
            }
            private void TryGetOemModelCode(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.oemModelCode != null
                                && jsonObject.oemModelCode.Value != null)
                    {
                        vehicle.OEMCode = jsonObject.oemModelCode.Value;
                    }
                }
                catch
                {
                    //TODO: Logging
                }

            }
            private void TryGetTrimAndStyle(dynamic jsonObject, VehicleEntity vehicle)
            {
                try
                {
                    if (vehicle == null) vehicle = new VehicleEntity();

                    if (jsonObject.style != null
                            && jsonObject.style.label != null
                            && jsonObject.style.id != null
                            && jsonObject.style.trim != null
                            && jsonObject.style.label.Value != null
                            && jsonObject.style.id.Value != null
                            && jsonObject.style.trim.Value!= null)
                    {

                        vehicle.Style = jsonObject.style.label.Value.ToString();
                        vehicle.StyleId = jsonObject.style.id.Value.ToString();
                        vehicle.Trim = jsonObject.style.trim.Value.ToString();
                    }
                }
                catch
                {
                    //TODO: take care of logging.
                }

            }

            private void TryGetColors(dynamic refStyles, List<ColorReferenceEntity> colorResults)
            {
                if (refStyles.colors != null && refStyles.colors.GetType() == typeof(JArray))
                {
                    foreach (var color in refStyles.colors)
                    {                        
                        var colorRefObject = new ColorReferenceEntity();                        

                        if (color.exterior != null )
                        {
                            var colorObject = new Color
                            {
                                Code = color.exterior.code ?? string.Empty,
                                //Base = color.exterior.base ?? string.Empty,
                                Name = color.exterior.name ?? string.Empty,
                                RgbHexCode = color.exterior.RGBHexCode ?? string.Empty
                            };
                            colorRefObject.ExternalColor = colorObject;

                        }
                        if (color.interior != null )
                        {
                            var colorObject = new Color
                            {
                                Code = color.interior.code ?? string.Empty,
                                //Base = color.exterior.base ?? string.Empty,
                                Name = color.interior.name ?? string.Empty,
                                RgbHexCode = color.interior.RGBHexCode ?? string.Empty
                            };
                            colorRefObject.InternalColor = new List<Color>(){
                                            colorObject
                                        };
                        }

                        colorResults.Add(colorRefObject);
                    }

                }

            }

        #endregion Parse individual Vehicle Objects from JSON
            
    }
}