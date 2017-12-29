using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		// todo:一つのサービスに色々まとめすぎてめんどくさいことになってるので、分離するなどする。

		/// <summary>
		/// Wifi,テザリングスイッチを押したときの各処理実装
		/// </summary>
		[IntentFilter(new string[] { ACTION_TOGGLE_WIFI, ACTION_TOGGLE_WIFI_AP })]
		[Service]   // AndroidManifestに追記する代わりにAttributeを設定する(xamarin)
		class DeviceSwitchService : Service
		{

			const string ACTION_TOGGLE_WIFI = "ToggleWifi";
			const string ACTION_TOGGLE_WIFI_AP = "ToggleWifiAp";

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


				// Wifiボタン押したらwifi機能ON,OFFする
				Intent ToggleWifiIntent = new Intent();
				// Wifiボタンを押したときにこのクラス宛にIntentブロードキャストして、OnStartCommandが呼ばれる。
				ToggleWifiIntent.SetAction(ACTION_TOGGLE_WIFI);
				// サービスクラスへ投げるインテント作成
				PendingIntent ToggleWifiPendingIntent = PendingIntent.GetService(this, 0, ToggleWifiIntent, 0);
				remoteViews.SetOnClickPendingIntent(Resource.Id.WiFiButton, ToggleWifiPendingIntent);

				// WifiApボタン押したらwifiAp機能On,Offする
				Intent ToggleWifiApIntent = new Intent();
				ToggleWifiApIntent.SetAction(ACTION_TOGGLE_WIFI_AP);
				PendingIntent ToggleWifiApPendingIntent = PendingIntent.GetService(this, 0, ToggleWifiApIntent, 0);
				remoteViews.SetOnClickPendingIntent(Resource.Id.TetheringButton, ToggleWifiApPendingIntent);

				// 上記Intentを受け取って呼び出された場合、以下を通る
				if (!string.IsNullOrEmpty(intent.Action)){
					if (intent.Action.Equals(ACTION_TOGGLE_WIFI)){
						Task.Run(() => ToggleWifiAsync());
					}

					if(intent.Action.Equals(ACTION_TOGGLE_WIFI_AP)) {
						Task.Run(() => ToggleWifiApAsync());
					}
				}
				else{	// Intent.Actionが空＝widgetのOnUpdateからの呼び出しとして以下を処理。
					if (WifiUtility.IsWifiEnabled(this)){
						remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_on);
					}
					else{
						remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_off);
					}
					if(WifiUtility.IsWifiApEnabled(this)) {
						remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_on);
					}
					else {
						remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_off);
					}
				}

				return remoteViews;
			}


			/// <summary>
			/// WifiのON,OFF切り替え
			/// </summary>
			async Task ToggleWifiAsync()
			{
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

			/// <summary>
			/// WifiAPのON,OFF切り替え
			/// </summary>
			/// <returns></returns>
			async Task<bool> ToggleWifiApAsync()
			{
				WifiApState apstate = WifiUtility.GetWifiApState(this);
				if(apstate == WifiApState.Failed) {
					var message = "This Device is not surpported WifiAP.";
					Toast.MakeText(this, message, ToastLength.Short).Show();
					return false;
				}

				if(WifiUtility.IsWifiApEnabled(this)) {
					await WifiUtility.ToggleWifiApAsync(this, false);
				}
				else {
					await WifiUtility.ToggleWifiApAsync(this, true);
				}

				bool result = false;



				return result;
			}
		}
	}
}