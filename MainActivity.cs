using Android.App;
using Android.Widget;
using Android.OS;

using Android.Net.Wifi;
using Android.Net;
using Android.Content;
//using Android.Net.Nsd

namespace NetworkDeviceSwitch
{
	[Activity(Label = "NetworkDeviceSwitch",MainLauncher = true,Icon = "@drawable/icon")]
	public class MainActivity:Activity
	{

		// NetworkStateReceiverのマニフェスト登録用文字列

		// ネットワーク切り替え時にブロードキャストされる
		public const string CONNECTIVITY_CHANGE = "android.net.conn.CONNECTIVITY_CHANGE";



		public TextView StatusView = null;

		Switch mWifiSwitch = null;		// Wifiスイッチ
		Switch mMobileSwitch = null;	// モバイルデータスイッチ
		Switch mTetheringSwitch = null;	// テザリングスイッチ

		ConnectivityManager	mConnectivityManager = null;

		WifiManager			mWifiManager = null;



		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

		// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);



			StatusView = FindViewById<TextView>(Resource.Id.StatusView);
			StatusView.Text = "";

			mWifiSwitch = FindViewById<Switch>(Resource.Id.WifiSwitch);
//			mWifiSwitch.Checked;
	//		mWifiSwitch.SetOnCheckedChangeListener(this);
			mWifiSwitch.CheckedChange += OnWifiSwitchCheckedChange;

			mConnectivityManager = (ConnectivityManager)GetSystemService(ConnectivityService);

			mWifiManager = (WifiManager)GetSystemService(WifiService);

			NetworkInfo activeNetworkInfo = mConnectivityManager.ActiveNetworkInfo;

			bool isOnline = (activeNetworkInfo != null) && activeNetworkInfo.IsConnected;
			if(isOnline) {
				switch(activeNetworkInfo.Type) {
				case ConnectivityType.Wifi:
					mWifiSwitch.Checked = true;
					break;
				default:
					break;
				}
			}

			// 通信情報確認
	//		CheckNetworkState();

			// このActivityの間だけブロードキャストされればいいので以下の方法で登録する
			// 通信状況取得用レシーバー登録
			var NetintentFilter = new IntentFilter();
			NetintentFilter.AddAction(CONNECTIVITY_CHANGE);
			NetworkStateReceiver NetStateReceiver = new NetworkStateReceiver(this, mConnectivityManager, mWifiManager);
			RegisterReceiver(NetStateReceiver, NetintentFilter);

		}

		/// <summary>
		/// Wifiスイッチ操作時コールバック
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnWifiSwitchCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			if(e.IsChecked) {
				if(ToggleWifi(e.IsChecked)) {
					Toast.MakeText(this, "Wifi Enabled.",ToastLength.Short).Show();
				}
			}
			else {
				if(ToggleWifi(e.IsChecked)) {
					Toast.MakeText(this, "Wifi Disabled.",ToastLength.Short).Show();
				}	
			}
		}


		bool ToggleWifi(bool flag)
		{
			bool result = false;
			if(mWifiManager.IsWifiEnabled != flag) {
				mWifiManager.SetWifiEnabled(flag);
				result = true;
			}

			return result;
		}

	}
}

