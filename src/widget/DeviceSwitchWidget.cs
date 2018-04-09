using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.Appwidget;
using Android.Net.Wifi;
using Android.Net;


/*
参考

https://starzero.hatenablog.com/entry/20110503/1304422342
http://y-anz-m.blogspot.jp/2011/07/androidappwidget-pendingintent-putextra.html
http://www110.kir.jp/Android/ch0408.html
http://workpiles.com/2014/01/android-notification-alarmmanager/
*/

namespace NetworkDeviceSwitch
{

	namespace Widget
	{
		/// <summary>
		/// Wifi,Tetheringの制御を行うウィジェットメイン
		/// </summary>
		[BroadcastReceiver(Label = "@string/WidgetName")]
		[IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate, MainActivity.CONNECTIVITY_CHANGE, MainActivity.SCAN_RESULTS, MainActivity.WIFI_STATE_CHANGE, MainActivity.WIFI_AP_STATE_CHANGE })]
		[MetaData("android.appwidget.provider", Resource = "@xml/widgetdefine")]    // ファイル名大文字でも、指定は小文字にしないとだめみたい
		class DeviceSwitchWidget : AppWidgetProvider
		{

			// Toggle Wifi Intent Action.
			public const string ACTION_TOGGLE_WIFI = "ToggleWifi";
			// Toggle Wifi Ap Intent Action.
			public const string ACTION_TOGGLE_WIFI_AP = "ToggleWifiAp";


			public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
			{
				base.OnUpdate(context, appWidgetManager, appWidgetIds);

				Update(context, appWidgetManager, appWidgetIds);
			}

			/// <summary>
			/// This is called when an instance the App Widget is created for the first time.
			/// </summary>
			/// <remarks>
			/// If the user adds two instances of your App Widget, this is only called the first time. 
			/// </remarks>
			/// <param name="context"></param>
			public override void OnEnabled(Context context)
			{
				base.OnEnabled(context);
				DebugUtility.LogInfo("WidgetStateReceiver", "WidgetStateReceiver::OnEnabled()");
			}


			/// <summary>
			/// AppWidgetManagerからのIntentを受け取って各On~Methodに処理を投げる
			/// </summary>
			/// <remarks>
			/// OnUpdateもOnEnabledなども全部まずここが呼ばれた上で、
			/// IntentのActionに合わせてOnUpdateなどに飛ばされる。
			/// 基本的な更新処理だけならOnUpdateに書くべきで、任意のコールバック処理を実装したい場合にここに追記する。
			/// </remarks>
			/// <param name="context"></param>
			/// <param name="intent"></param>
			public override void OnReceive(Context context, Intent intent)
			{
				base.OnReceive(context, intent);

				// Wifiのステートの切り替わりでボタンのON,OFF表示切り替える
				if(intent.Action.Equals(MainActivity.WIFI_STATE_CHANGE)){
					var wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
					RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.WidgetLayout);

					if (wifiManager.WifiState == WifiState.Enabled){
						remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_on);
					}
					else if(wifiManager.WifiState == WifiState.Disabled){
						remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_off);
					}

					ComponentName widget = new ComponentName(context, Java.Lang.Class.FromType(typeof(DeviceSwitchWidget)).Name);
					AppWidgetManager manager = AppWidgetManager.GetInstance(context);
					manager.UpdateAppWidget(widget, remoteViews);
				}

				if(intent.Action.Equals(MainActivity.WIFI_AP_STATE_CHANGE)) {
					var wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
					RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.WidgetLayout);

					// WifiApStateは、WifiApEnabledがすでにfalseだったとしてもまだDisabling状態の場合があるので
					// ここではWifiApEnabledでボタンの表示切り替えを判定する.
//					WifiApState state = WifiUtility.GetWifiApState(context);
					bool wifiapEnabled = WifiUtility.IsWifiApEnabled(context);
					if(wifiapEnabled) {
						remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_on);
					}
					else if(wifiapEnabled == false) {
						remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_off);
					}

					ComponentName widget = new ComponentName(context, Java.Lang.Class.FromType(typeof(DeviceSwitchWidget)).Name);
					AppWidgetManager manager = AppWidgetManager.GetInstance(context);
					manager.UpdateAppWidget(widget, remoteViews);
				}
			}

			/// <summary>
			/// This is called when the last instance of your App Widget is deleted from the App Widget host.
			/// This is where you should clean up any work done in onEnabled(Context), such as delete a temporary database.
			/// </summary>
			/// <param name="context"></param>
			public override void OnDisabled(Context context)
			{
				base.OnDisabled(context);
				DebugUtility.LogInfo("WidgetStateReceiver", "WidgetStateReceiver::OnDisabled()");
			}


			/// <summary>
			/// ウィジェットのアップデート処理
			/// </summary>
			/// <param name="context"></param>
			/// <param name="appWidgetManager"></param>
			/// <param name="appWidgetIds"></param>
			private void Update(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
			{
				RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.WidgetLayout);

				DebugUtility.LogInfo("Info", $"appWidgetIds.Length = {appWidgetIds.Length}");

				foreach(var appWidgetId in appWidgetIds){

					DebugUtility.LogInfo("Info", $"appWidgetId = {appWidgetId}");

					// Wifiスイッチを押したときにON,OFF制御するサービスにインテントを送る
					// インテントを送る先を指定
					Intent ToggleWifiIntent = new Intent(context, typeof(WifiWidgetService));
					ToggleWifiIntent.SetAction(ACTION_TOGGLE_WIFI);
					ToggleWifiIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
					PendingIntent ToggleWifiPendingIntent = PendingIntent.GetService(context, appWidgetId, ToggleWifiIntent, PendingIntentFlags.UpdateCurrent);
					remoteViews.SetOnClickPendingIntent(Resource.Id.WiFiButton, ToggleWifiPendingIntent);

					// WifiApスイッチを押したときにON,OFF制御するサービスにインテントを送る
					Intent ToggleWifiApIntent = new Intent(context, typeof(WifiApWidgetService));
					ToggleWifiApIntent.SetAction(ACTION_TOGGLE_WIFI_AP);
					ToggleWifiApIntent.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
					PendingIntent ToggleWifiApPendingIntent = PendingIntent.GetService(context, appWidgetId, ToggleWifiApIntent, PendingIntentFlags.UpdateCurrent);
					remoteViews.SetOnClickPendingIntent(Resource.Id.TetheringButton, ToggleWifiApPendingIntent);
				}

				// ウィジェットのボタンイメージの切り替え
				if (WifiUtility.IsWifiEnabled(context)){
					remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_on);
				}
				else{
					remoteViews.SetImageViewResource(Resource.Id.WiFiButton, Resource.Drawable.wifi_button_off);
				}
				if(WifiUtility.IsWifiApEnabled(context)) {
					remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_on);
				}
				else {
					remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_off);
				}

				appWidgetManager.UpdateAppWidget(appWidgetIds, remoteViews);
			}
		}
	}

}