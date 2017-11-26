using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Appwidget;
using Android.Net.Wifi;

namespace NetworkDeviceSwitch
{

	namespace Widget
	{


		/// <summary>
		/// Wifi,テザリングスイッチを押したときの各処理実装
		/// </summary>
		[IntentFilter(new string[] { ACTION_TOGGLE_WIFI })]
		[Service]   // AndroidManifestに追記する代わりにAttributeを設定する(xamarin)
		class DeviceSwitchService : Service
		{

			const string ACTION_TOGGLE_WIFI = "ToggleWifi";

			WifiManager _WifiManager = null;


			public override void OnCreate()
			{
				base.OnCreate();

				_WifiManager = (WifiManager)GetSystemService(WifiService);
			}

			public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
			{

				RemoteViews remoteViews = BuildUpdate(intent);

				ComponentName widget = new ComponentName(this, Java.Lang.Class.FromType(typeof(DeviceSwitchWidget)).Name);
				AppWidgetManager manager = AppWidgetManager.GetInstance(this);
				manager.UpdateAppWidget(widget, remoteViews);

				//			Toast.MakeText(this, "DeviceSwitchService:OnstartCommand", ToastLength.Long).Show();

				return StartCommandResult.Sticky;
			}


			public override IBinder OnBind(Intent intent)
			{
				// バインドされたくない場合はnullを返す
				return null;
			}

			RemoteViews BuildUpdate(Intent intent)
			{
				RemoteViews remoteViews = new RemoteViews(this.PackageName, Resource.Layout.WidgetLayout);


				// ボタン押したらwifiスイッチON,OFFする
				Intent ToggleWifiIntent = new Intent();
				// ボタンを押したときにこのIntentがブロードキャストされて、それをこのクラスが直接受け取り、OnStartCommandが呼ばれる。
				ToggleWifiIntent.SetAction(ACTION_TOGGLE_WIFI);
				PendingIntent ToggleWifiPendingIntent = PendingIntent.GetService(this, 0, ToggleWifiIntent, 0);   // サービスクラスへ投げるインテント作成
				remoteViews.SetOnClickPendingIntent(Resource.Id.WiFiButton, ToggleWifiPendingIntent);

				if (!string.IsNullOrEmpty(intent.Action)){
					if (intent.Action.Equals(ACTION_TOGGLE_WIFI)){
						ToggleWifi();
					}
				}

				if (_WifiManager.IsWifiEnabled){
					remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_on);
				}
				else{
					remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_off);
				}

				return remoteViews;
			}


			/// <summary>
			/// WifiのON,OFF切り替え
			/// </summary>
			void ToggleWifi()
			{
				if (_WifiManager.IsWifiEnabled)
				{
					_WifiManager.SetWifiEnabled(false);
		//			Toast.MakeText(this, "Wifi Disabled.", ToastLength.Short).Show();
				}
				else
				{
					_WifiManager.SetWifiEnabled(true);
		//			Toast.MakeText(this, "Wifi Enabled.", ToastLength.Short).Show();
				}
			}
		}
	}
}