using Android.Content;
using Android.Net.Wifi;
using Android.Net;
using Android.App;
using Android.Widget;
using System;

namespace NetworkDeviceSwitch
{
	/// <summary>
	/// 通信状態の切り替わり時にブロードキャストされ、その時のネットワークデバイス情報を取得、表示するクラス
	/// 以下の登録方法はアプリがバックグラウンドにいる間もブロードキャストされるので
	/// 今回はそこまで必要ないのでActivity内でインスタンス化して登録する
	/// </summary>
//	[BroadcastReceiver]
//	[IntentFilter(new[] { MainActivity.CONNECTIVITY_CHANGE })]
	public class NetworkStateReceiver : BroadcastReceiver
	{
		MainActivity		mParent = null;

		ConnectivityManager	mConnectivityManager = null;

		WifiManager			mWifiManager = null;


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NetworkStateReceiver(MainActivity parent, ConnectivityManager connectivity, WifiManager wifi)
		{
			mParent = parent;

			mConnectivityManager = connectivity;

			mWifiManager = wifi;
		}

		/// <summary>
		/// ブロードキャスト受取
		/// UIスレッドで実行される
		/// </summary>
		/// <param name="context"></param>
		/// <param name="intent"></param>
		public override void OnReceive(Context context, Intent intent)
		{
			System.Diagnostics.Debug.WriteLine("接続に変化がありました");
			CheckNetworkState();
		}

		/// <summary>
		/// 通信情報確認
		/// </summary>
		void CheckNetworkState()
		{
			mParent.StatusView.Text = "";

			NetworkInfo activeNetworkInfo = mConnectivityManager.ActiveNetworkInfo;

			bool isOnline = (activeNetworkInfo != null) && activeNetworkInfo.IsConnected;

			mParent.StatusView.Text += string.Format("NetworkState : {0}\n", (isOnline ? "Online" : "Offline"));

			if(isOnline) {
				mParent.StatusView.Text += string.Format("ConnectType : {0}\n", activeNetworkInfo.TypeName);

				switch(activeNetworkInfo.Type) {
				case ConnectivityType.Wifi:
					WifiInfo info = mWifiManager.ConnectionInfo;
					mParent.StatusView.Text += string.Format("BSSID : {0}\n", info.BSSID);
					mParent.StatusView.Text += string.Format("SSID : {0}\n", info.SSID);

					byte[] byteArray = BitConverter.GetBytes(info.IpAddress);
					Java.Net.InetAddress inetAddress = Java.Net.InetAddress.GetByAddress(byteArray);
					string ipaddress = inetAddress.HostAddress;
					mParent.StatusView.Text += string.Format("IpAddress : {0}\n", ipaddress);
					break;
				case ConnectivityType.Mobile:
					break;
				default: break;
				}
			}
		}
	}

}
