using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XLua;

// 下载管理，将下载单独放入到一个线程，每次处理固定数量的下载任务，减轻内存压力和服务器压力
public class DownloadMgr
{

    // 最大的下载数量
#if UNITY_EDITOR
    private static int _MaxCount = 20;
#else
    private static int _MaxCount = 10;
#endif
    // 当前正在下载的数量
    private static int _workingCount = 0;

	/*
	 * 	单例模式
	 * */
	public static DownloadMgr getInstance()
	{
		if (instance == null)
		{
			instance = new DownloadMgr();
		}
		return instance;
	}

	private static DownloadMgr instance = null;

	private DownloadMgr()
	{
	}

	// 是否已经启动
	public bool isActive = false;
	// 下载队列
	List<DownloadFileInfo> list = new List<DownloadFileInfo>();
	// 队列锁
	private readonly object syncLock = new object();
	// 阻塞
	AutoResetEvent waitHandle = new AutoResetEvent(false);

	// 进行初始化操作
	public void init() {
		isActive = true;

		Thread t = new Thread (work);
		t.Start ();
	}

	public void work() {
		DownloadMgr mgr = DownloadMgr.getInstance ();

		while (mgr.isActive) {
			if (!mgr.trigger ()) {
				Thread.Sleep (3000);
			}
		}
	}

	public bool trigger () {
		DownloadFileInfo data = null;
		bool empty = true;

		lock(syncLock) {
			if (list.Count > 0) {
				data = list [0];
				list.RemoveAt (0);

				empty = false;
			}
		}

		if (!empty) {
			doDownload (data);
		}

		return !empty;
	}

	private void doDownload(DownloadFileInfo data) {
        while (_workingCount >= _MaxCount) {
            waitHandle.WaitOne();
        }
        _workingCount++;

		OtkClient.DownloadFile (data);
	}

    public void addDownload(string fileURL, string savePath, DownloadCallback succeedCallback, DownloadCallback failedCallback, LuaFunction progressCallback) {
		lock (syncLock) {
			DownloadFileInfo info = new DownloadFileInfo ();
			info.fileURL = fileURL;
			info.realFilePath = savePath;

            // 先下载到一个临时文件，然后再复制到正确路径
            info.tmpFilePath = savePath + GameController.getInstance().getMilliseconds();

			info.succeedCallback = ()=>{
				succeedCallback();

                onDownloadOver();
			};
			info.failedCallback = () => {
				failedCallback ();

                onDownloadOver();
			};
            info.progressCallback = progressCallback;

			list.Add (info);
		}
	}

    private void onDownloadOver() {
        _workingCount--;

        waitHandle.Set();
    }
}
