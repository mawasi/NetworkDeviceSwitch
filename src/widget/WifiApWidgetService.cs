using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Widget;

namespace NetworkDeviceSwitch
{
	namespace Widget
	{
		/// <summary>
		/// WidgetのWifiApボタンを押したときに動くサービス
		/// </summary>
		[Service]
		class WifiApWidgetService : Service
		{
			[return: GeneratedEnum]
			public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
			{
				// 以下のインテントを受け取った場合、別スレッドにWifiAp有効化処理投げる
				if(!string.IsNullOrEmpty(intent.Action)){
					if(intent.Action.Equals(DeviceSwitchWidget.ACTION_TOGGLE_WIFI_AP)){
						Task.Run(ToggleWifiApAsync);
					}
				}

				return base.OnStartCommand(intent, flags, startId);
			}

			public override IBinder OnBind(Intent intent)
			{
				// バインドされたくない場合はnullを返す
				return null;
			}


			/// <summary>
			/// WifiAPのON,OFF切り替え
			/// </summary>
			/// <returns></returns>
			async Task<bool> ToggleWifiApAsync()
			{
				WifiApState apstate = WifiUtility.GetWifiApState(this);
				if(apstate == WifiApState.Failed) {
					return false;
				}

				if(WifiUtility.IsWifiApEnabled(this)) {
					await WifiUtility.ToggleWifiApAsync(this, false);
				}
				else {
					await WifiUtility.ToggleWifiApAsync(this, true);
				}

				return true;
			}
		}
	}
}