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

namespace NetworkDeviceSwitch
{

	namespace Widget
	{
		/// <summary>
		/// ウィジェットメイン
		/// </summary>
		[BroadcastReceiver(Label = "@string/WidgetName")]
		[IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
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
			}

			/// <summary>
			/// This is called when the last instance of your App Widget is deleted from the App Widget host.
			/// This is where you should clean up any work done in onEnabled(Context), such as delete a temporary database.
			/// </summary>
			/// <param name="context"></param>
			public override void OnDisabled(Context context)
			{
				base.OnDisabled(context);
			}
		}
	}

}