using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.Net.Wifi;
using Android.Appwidget;

namespace NetworkDeviceSwitch
{
	namespace Widget
	{
		/// <summary>
		/// デバイスの状態に合わせてウィジェットの見た目を切り替えるためのレシーバー
		/// </summary>
		[BroadcastReceiver(Enabled = true, Exported = true)]	// 外部アプリからのブロードキャストも受け取って状態切り替えたいので Exported=true
		[IntentFilter(new[] { MainActivity.CONNECTIVITY_CHANGE, MainActivity.SCAN_RESULTS, MainActivity.WIFI_STATE_CHANGE })]
		class WidgetStateReceiver : BroadcastReceiver
		{

			/// <summary>
			/// デフォルトコンストラクタ
			/// </summary>
			public WidgetStateReceiver() { }

			public override void OnReceive(Context context, Intent intent)
			{

				var wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);

				RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.WidgetLayout);

				if (wifiManager.IsWifiEnabled){
					remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_on);
					Android.Util.Log.Info("WidgetStateReceiver", "Wifi Enabled.");
				}
				else{
					remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_off);
					Android.Util.Log.Info("WidgetStateReceiver", "Wifi Disabled.");
				}

				ComponentName widget = new ComponentName(context, Java.Lang.Class.FromType(typeof(DeviceSwitchWidget)).Name);
				AppWidgetManager manager = AppWidgetManager.GetInstance(context);
				manager.UpdateAppWidget(widget, remoteViews);
			}
		}
	}
}