using GalaSoft.MvvmLight;
using Cyclestreets.Managers;
using GalaSoft.MvvmLight.Ioc;
namespace Cyclestreets.ViewModel
{
	public class DirectionsPageViewModel : ViewModelBase
	{
        public RouteManager RouteManagerPtr
        {
            get 
            {
                return SimpleIoc.Default.GetInstance<RouteManager>();
            }
        }
		
		public DirectionsPageViewModel()
		{
			if( IsInDesignMode )
			{
                string sampleJSON  = @"{
   ""marker"":[
      {
         ""@attributes"":{
            ""start"":""Link joining St Mary's Passage, King's Parade, NCN 11, King's Parade"",
            ""finish"":""Thoday Street"",
            ""startBearing"":""0"",
            ""startSpeed"":""0"",
            ""start_longitude"":""0.117950"",
            ""start_latitude"":""52.205303"",
            ""finish_longitude"":""0.147324"",
            ""finish_latitude"":""52.199650"",
            ""crow_fly_distance"":""2099"",
            ""event"":""depart"",
            ""whence"":""2010-11-11 22:58:45"",
            ""speed"":""20"",
            ""itinerary"":""345529"",
            ""clientRouteId"":""0"",
            ""plan"":""fastest"",
            ""note"":"""",
            ""length"":""3275"",
            ""time"":""776"",
            ""busynance"":""5338"",
            ""quietness"":""61"",
            ""signalledJunctions"":""2"",
            ""signalledCrossings"":""0"",
            ""west"":""0.117867"",
            ""south"":""52.198071"",
            ""east"":""0.14863"",
            ""north"":""52.206089"",
            ""name"":""Link joining St Mary's Passage, King's Parade, NCN 11, King's Parade to Thoday Street"",
            ""walk"":""0"",
            ""leaving"":""2010-11-11 22:58:45"",
            ""arriving"":""2010-11-11 23:11:41"",
            ""coordinates"":""0.117867,52.205288 0.117872,52.205441 0.117904,52.205482 0.117978,52.205502 0.118032,52.205448 0.118107,52.205437 0.118507,52.205463 0.118734,52.205505 0.118932,52.20557 0.11916,52.20565 0.119246,52.205681 0.119653,52.205841 0.120082,52.205971 0.120563,52.206089 0.120631,52.206085 0.120683,52.206078 0.12073,52.206059 0.120959,52.205765 0.121141,52.205536 0.121251,52.205444 0.121492,52.205345 0.121741,52.205292 0.121827,52.205273 0.122161,52.205009 0.122373,52.204815 0.122983,52.204117 0.123057,52.204041 0.123854,52.204529 0.124476,52.205055 0.125591,52.204685 0.127306,52.204071 0.128187,52.203739 0.12837,52.203659 0.128829,52.20348 0.129657,52.203156 0.130846,52.202679 0.13105,52.202591 0.131111,52.202591 0.13117,52.202599 0.131243,52.202633 0.131241,52.202564 0.131236,52.202515 0.131654,52.202339 0.13228,52.202076 0.132418,52.202019 0.132928,52.201805 0.133287,52.20166 0.134104,52.201328 0.134795,52.201027 0.135064,52.200916 0.135563,52.20071 0.136058,52.200508 0.136321,52.200405 0.136742,52.200253 0.136892,52.200195 0.137123,52.200104 0.137288,52.200035 0.138386,52.199593 0.138401,52.199581 0.13858,52.199516 0.139368,52.199215 0.139387,52.199207 0.139815,52.199059 0.140267,52.198929 0.141393,52.198555 0.142005,52.198376 0.142015,52.198376 0.142917,52.198116 0.143034,52.198082 0.143084,52.198071 0.143176,52.198288 0.143718,52.199577 0.143734,52.199615 0.143814,52.199593 0.145211,52.199341 0.145323,52.199333 0.145412,52.199303 0.146076,52.199188 0.146166,52.1992 0.14743,52.201195 0.147501,52.20137 0.147463,52.201561 0.147353,52.201736 0.147199,52.201836 0.147,52.201851 0.14705,52.201942 0.147017,52.202061 0.147167,52.202114 0.147386,52.202114 0.147506,52.202061 0.147635,52.20203 0.147788,52.202026 0.148368,52.20211 0.148448,52.202164 0.14863,52.202053 0.148542,52.202011 0.148537,52.201923 0.148601,52.201584 0.148498,52.201328 0.148237,52.200905 0.147489,52.19968"",
            ""grammesCO2saved"":""610"",
            ""calories"":""53"",
            ""type"":""route""
         }
      },
      {
         ""@attributes"":{
            ""name"":""King's Parade"",
            ""legNumber"":""0"",
            ""distance"":""27"",
            ""time"":""5"",
            ""busynance"":""40"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear left"",
            ""startBearing"":""1"",
            ""color"":""#33aa33"",
            ""points"":""0.117867,52.205288 0.117872,52.205441 0.117904,52.205482 0.117978,52.205502"",
            ""distances"":""0,17,5,5"",
            ""elevations"":""17,17,17,17"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""St Mary's Street, NCN 11"",
            ""legNumber"":""0"",
            ""distance"":""55"",
            ""time"":""10"",
            ""busynance"":""79"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""sharp right"",
            ""startBearing"":""148"",
            ""color"":""#33aa33"",
            ""points"":""0.117978,52.205502 0.118032,52.205448 0.118107,52.205437 0.118507,52.205463 0.118734,52.205505"",
            ""distances"":""0,7,5,27,16"",
            ""elevations"":""17,17,17,17,17"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Market Street, NCN 11"",
            ""legNumber"":""0"",
            ""distance"":""141"",
            ""time"":""39"",
            ""busynance"":""299"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""straight on"",
            ""startBearing"":""62"",
            ""color"":""#33aa33"",
            ""points"":""0.118734,52.205505 0.118932,52.20557 0.11916,52.20565 0.119246,52.205681 0.119653,52.205841 0.120082,52.205971 0.120563,52.206089"",
            ""distances"":""0,15,18,7,33,33,35"",
            ""elevations"":""17,17,17,17,19,19,19"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Sidney Street"",
            ""legNumber"":""0"",
            ""distance"":""13"",
            ""time"":""3"",
            ""busynance"":""13"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear right"",
            ""startBearing"":""95"",
            ""color"":""#ff0000"",
            ""points"":""0.120563,52.206089 0.120631,52.206085 0.120683,52.206078 0.12073,52.206059"",
            ""distances"":""0,5,4,4"",
            ""elevations"":""19,19,19,19"",
            ""provisionName"":""Cycle Track"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Sidney Street"",
            ""legNumber"":""0"",
            ""distance"":""121"",
            ""time"":""18"",
            ""busynance"":""135"",
            ""flow"":""against"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear right"",
            ""startBearing"":""154"",
            ""color"":""#33aa33"",
            ""points"":""0.12073,52.206059 0.120959,52.205765 0.121141,52.205536 0.121251,52.205444 0.121492,52.205345 0.121741,52.205292 0.121827,52.205273"",
            ""distances"":""0,36,28,13,20,18,6"",
            ""elevations"":""19,19,19,18,16,16,16"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""St Andrew's Street"",
            ""legNumber"":""0"",
            ""distance"":""161"",
            ""time"":""40"",
            ""busynance"":""311"",
            ""flow"":""against"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""straight on"",
            ""startBearing"":""142"",
            ""color"":""#33aa33"",
            ""points"":""0.121827,52.205273 0.122161,52.205009 0.122373,52.204815 0.122983,52.204117 0.123057,52.204041"",
            ""distances"":""0,37,26,88,10"",
            ""elevations"":""16,15,15,17,17"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Emmanuel Street"",
            ""legNumber"":""0"",
            ""distance"":""149"",
            ""time"":""23"",
            ""busynance"":""182"",
            ""flow"":""against"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""turn left"",
            ""startBearing"":""45"",
            ""color"":""#33aa33"",
            ""points"":""0.123057,52.204041 0.123854,52.204529 0.124476,52.205055"",
            ""distances"":""0,77,72"",
            ""elevations"":""17,17,15"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Drummer Street"",
            ""legNumber"":""0"",
            ""distance"":""86"",
            ""time"":""34"",
            ""busynance"":""266"",
            ""flow"":""against"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""turn right"",
            ""startBearing"":""118"",
            ""color"":""#33aa33"",
            ""points"":""0.124476,52.205055 0.125591,52.204685"",
            ""distances"":""0,86"",
            ""elevations"":""15,18"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Parker Street"",
            ""legNumber"":""0"",
            ""distance"":""135"",
            ""time"":""23"",
            ""busynance"":""178"",
            ""flow"":""against"",
            ""walk"":""0"",
            ""signalledJunctions"":""1"",
            ""signalledCrossings"":""0"",
            ""turn"":""straight on"",
            ""startBearing"":""120"",
            ""color"":""#33aa33"",
            ""points"":""0.125591,52.204685 0.127306,52.204071"",
            ""distances"":""0,135"",
            ""elevations"":""18,17"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Parkside"",
            ""legNumber"":""0"",
            ""distance"":""303"",
            ""time"":""94"",
            ""busynance"":""571"",
            ""flow"":""against"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""straight on"",
            ""startBearing"":""122"",
            ""color"":""#33aa33"",
            ""points"":""0.127306,52.204071 0.128187,52.203739 0.12837,52.203659 0.128829,52.20348 0.129657,52.203156 0.130846,52.202679 0.13105,52.202591"",
            ""distances"":""0,70,15,37,67,97,17"",
            ""elevations"":""17,20,20,19,16,17,17"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Link between Parkside and East Road"",
            ""legNumber"":""0"",
            ""distance"":""14"",
            ""time"":""3"",
            ""busynance"":""56"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear left"",
            ""startBearing"":""90"",
            ""color"":""#3333aa"",
            ""points"":""0.13105,52.202591 0.131111,52.202591 0.13117,52.202599 0.131243,52.202633"",
            ""distances"":""0,4,4,6"",
            ""elevations"":""17,17,17,17"",
            ""provisionName"":""Busy and fast road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Link between Mill Road and East Road"",
            ""legNumber"":""0"",
            ""distance"":""14"",
            ""time"":""4"",
            ""busynance"":""56"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""sharp right"",
            ""startBearing"":""181"",
            ""color"":""#3333aa"",
            ""points"":""0.131243,52.202633 0.131241,52.202564 0.131236,52.202515"",
            ""distances"":""0,8,6"",
            ""elevations"":""17,17,17"",
            ""provisionName"":""Busy and fast road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Mill Road"",
            ""legNumber"":""0"",
            ""distance"":""951"",
            ""time"":""227"",
            ""busynance"":""1549"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""1"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear left"",
            ""startBearing"":""124"",
            ""color"":""#33aa33"",
            ""points"":""0.131236,52.202515 0.131654,52.202339 0.13228,52.202076 0.132418,52.202019 0.132928,52.201805 0.133287,52.20166 0.134104,52.201328 0.134795,52.201027 0.135064,52.200916 0.135563,52.20071 0.136058,52.200508 0.136321,52.200405 0.136742,52.200253 0.136892,52.200195 0.137123,52.200104 0.137288,52.200035 0.138386,52.199593 0.138401,52.199581 0.13858,52.199516 0.139368,52.199215 0.139387,52.199207 0.139815,52.199059 0.140267,52.198929 0.141393,52.198555 0.142005,52.198376 0.142015,52.198376 0.142917,52.198116 0.143034,52.198082 0.143084,52.198071"",
            ""distances"":""0,35,52,11,42,29,67,58,22,41,41,21,33,12,19,14,90,2,14,63,2,34,34,87,46,1,68,9,4"",
            ""elevations"":""17,17,19,19,19,19,19,19,19,19,19,19,19,19,19,19,18,20,20,19,19,18,18,19,19,19,18,18,18"",
            ""provisionName"":""Road"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Cavendish Road"",
            ""legNumber"":""0"",
            ""distance"":""177"",
            ""time"":""33"",
            ""busynance"":""210"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""turn left"",
            ""startBearing"":""15"",
            ""color"":""#000000"",
            ""points"":""0.143084,52.198071 0.143176,52.198288 0.143718,52.199577 0.143734,52.199615"",
            ""distances"":""0,25,148,4"",
            ""elevations"":""18,18,18,18"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""St Philip's Road"",
            ""legNumber"":""0"",
            ""distance"":""173"",
            ""time"":""49"",
            ""busynance"":""301"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""turn right"",
            ""startBearing"":""115"",
            ""color"":""#000000"",
            ""points"":""0.143734,52.199615 0.143814,52.199593 0.145211,52.199341 0.145323,52.199333 0.145412,52.199303 0.146076,52.199188 0.146166,52.1992"",
            ""distances"":""0,6,99,8,7,47,6"",
            ""elevations"":""18,19,17,17,17,19,19"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Catharine Street"",
            ""legNumber"":""0"",
            ""distance"":""329"",
            ""time"":""59"",
            ""busynance"":""383"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""turn left"",
            ""startBearing"":""21"",
            ""color"":""#000000"",
            ""points"":""0.146166,52.1992 0.14743,52.201195 0.147501,52.20137 0.147463,52.201561 0.147353,52.201736 0.147199,52.201836 0.147,52.201851"",
            ""distances"":""0,238,20,21,21,15,14"",
            ""elevations"":""19,16,15,15,15,15,16"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Link joining Roundabout, Cromwell Road, Catharine Street"",
            ""legNumber"":""0"",
            ""distance"":""11"",
            ""time"":""2"",
            ""busynance"":""13"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear right"",
            ""startBearing"":""19"",
            ""color"":""#000000"",
            ""points"":""0.147,52.201851 0.14705,52.201942"",
            ""distances"":""0,11"",
            ""elevations"":""16,16"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Roundabout"",
            ""legNumber"":""0"",
            ""distance"":""50"",
            ""time"":""22"",
            ""busynance"":""133"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""bear left"",
            ""startBearing"":""350"",
            ""color"":""#000000"",
            ""points"":""0.14705,52.201942 0.147017,52.202061 0.147167,52.202114 0.147386,52.202114 0.147506,52.202061"",
            ""distances"":""0,13,12,15,10"",
            ""elevations"":""16,16,13,13,15"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Fairfax Road"",
            ""legNumber"":""0"",
            ""distance"":""85"",
            ""time"":""19"",
            ""busynance"":""115"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""sharp right"",
            ""startBearing"":""111"",
            ""color"":""#000000"",
            ""points"":""0.147506,52.202061 0.147635,52.20203 0.147788,52.202026 0.148368,52.20211 0.148448,52.202164 0.14863,52.202053"",
            ""distances"":""0,9,10,41,8,17"",
            ""elevations"":""15,15,15,13,13,14"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      },
      {
         ""@attributes"":{
            ""name"":""Thoday Street"",
            ""legNumber"":""0"",
            ""distance"":""280"",
            ""time"":""69"",
            ""busynance"":""448"",
            ""flow"":""with"",
            ""walk"":""0"",
            ""signalledJunctions"":""0"",
            ""signalledCrossings"":""0"",
            ""turn"":""sharp right"",
            ""startBearing"":""232"",
            ""color"":""#000000"",
            ""points"":""0.14863,52.202053 0.148542,52.202011 0.148537,52.201923 0.148601,52.201584 0.148498,52.201328 0.148237,52.200905 0.147489,52.19968"",
            ""distances"":""0,8,10,38,29,50,145"",
            ""elevations"":""14,14,14,14,14,16,17"",
            ""provisionName"":""Quiet Street"",
            ""type"":""segment""
         }
      }
   ],
   ""waypoint"":[
      {
         ""@attributes"":{
            ""longitude"":""0.117950"",
            ""latitude"":""52.205303"",
            ""sequenceId"":""1""
         }
      },
      {
         ""@attributes"":{
            ""longitude"":""0.147324"",
            ""latitude"":""52.199650"",
            ""sequenceId"":""2""
         }
      }
   ]
}";
			    RouteManagerPtr.ParseRouteData(sampleJSON, "fastest", true);

			}
			else
			{
			
			}
		}

		
	}
}
