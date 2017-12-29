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

namespace NetworkDeviceSwitch
{

	namespace Widget
	{
		/// <summary>
		/// ウィジェットメイン
		/// </summary>
		[BroadcastReceiver(Label = "@string/WidgetName")]
		[IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate, MainActivity.CONNECTIVITY_CHANGE, MainActivity.SCAN_RESULTS, MainActivity.WIFI_STATE_CHANGE, MainActivity.WIFI_AP_STATE_CHANGE })]
		[MetaData("android.appwidget.provider", Resource = "@xml/widgetdefine")]    // ファイル名大文字でも、指定は小文字にしないとだめみたい
		class DeviceSwitchWidget : AppWidgetProvider
		{
			public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
			{
				base.OnUpdate(context, appWidgetManager, appWidgetIds);

				//			context.GetSystemService


				context.StartService(new Intent(context, typeof(DeviceSwitchService)));
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
				Android.Util.Log.Info("WidgetStateReceiver", "WidgetStateReceiver::OnEnabled()");
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

					WifiApState state = WifiUtility.GetWifiApState(context);
					if(state == WifiApState.Enabled) {
						remoteViews.SetImageViewResource(Resource.Id.TetheringButton, Resource.Drawable.ap_button_on);
					}
					else if(state == WifiApState.Disabled) {
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
				Android.Util.Log.Info("WidgetStateReceiver", "WidgetStateReceiver::OnDisabled()");
			}
		}
	}

}