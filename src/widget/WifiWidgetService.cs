using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Net.Wifi;


namespace NetworkDeviceSwitch
{
	namespace Widget
	{
		/// <summary>
		/// WidgetのWifiボタン押したときに動くサービス
		/// </summary>
		[Service]
		class WifiWidgetService : Service
		{
			[return: GeneratedEnum]
			public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
			{
				// 以下のアクションでインテントを受け取った場合に別スレッドにWifi機能の切り替え処理を投げる
				if(!string.IsNullOrEmpty(intent.Action)){
					if(intent.Action.Equals(DeviceSwitchWidget.ACTION_TOGGLE_WIFI)){
						Task.Run(ToggleWifiAsync);
					}
				}

	//			return base.OnStartCommand(intent, flags, startId);
				return StartCommandResult.Sticky;
			}

			public override IBinder OnBind(Intent intent)
			{
				return null;
			}

			/// <summary>
			/// WifiのON,OFF切り替え
			/// </summary>
			async Task ToggleWifiAsync()
			{
				var _WifiManager = (WifiManager)GetSystemService(WifiService);

				if (_WifiManager.IsWifiEnabled){
					_WifiManager.SetWifiEnabled(false);
				}
				else{
					_WifiManager.SetWifiEnabled(true);
				}

				if(WifiUtility.IsWifiEnabled(this)) {
					await WifiUtility.ToggleWifiAsync(this, false);
				}
				else {
					await WifiUtility.ToggleWifiAsync(this, true);
				}

			}
		}
	}
}