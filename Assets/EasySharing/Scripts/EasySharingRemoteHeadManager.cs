using HoloToolkit.Sharing;
using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EasySharingRemoteHeadManager : Singleton<EasySharingRemoteHeadManager> {

    /// <summary>
    /// リモートユーザを表す内部クラス
    /// </summary>
    public class RemoteHeadInfo {
        public long UserID;
        public GameObject HeadObject;
    }

    /// <summary>
    /// 同じルームにいるリモートユーザの辞書
    /// ユーザIDをキーとしている
    /// </summary>
    private Dictionary<long, RemoteHeadInfo> remoteHeads = new Dictionary<long, RemoteHeadInfo>();

    private void Start() {
        // HeadTransformタイプのメッセージを受信したときのハンドラを設定する
        EasySharingMessages.Instance.MessageHandlers[EasySharingMessages.TestMessageID.HeadTransform] = UpdateHeadTransform;

        if(SharingStage.Instance.IsConnected) {
            Connected();
        } else {
            SharingStage.Instance.SharingManagerConnected += Connected;
        }
    }


    /// <summary>
    /// サーバへの接続完了時のハンドラ
    /// ユーザがセッションに出入りしたときのハンドラを設定する
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Connected(object sender = null, EventArgs e = null) {
        SharingStage.Instance.SharingManagerConnected -= Connected;

        SharingStage.Instance.SessionUsersTracker.UserJoined += UserJoinedSession;
        SharingStage.Instance.SessionUsersTracker.UserLeft += UserLeftSession;
    }

    #region User Joined
    /// <summary>
    /// リモートユーザがセッションに入ってきたときのハンドラ
    /// リストに入れるオブジェクトを生成し、シーンに配置するGameObjectを生成する
    /// </summary>
    /// <param name="user"></param>
    private void UserJoinedSession(User user) {
        int userId = user.GetID();  // セッションにinしたユーザのID
        if(userId != SharingStage.Instance.Manager.GetLocalUser().GetID()) {
            GetRemoteHeadInfo(userId);
        }
    }

    /// <summary>
    /// リストに入れるリモートユーザの情報を生成する
    /// たぶんありえないけど、既存のものがいたら流用
    /// </summary>
    /// <param name="userId"></param>
    private RemoteHeadInfo GetRemoteHeadInfo(long userId) {
        RemoteHeadInfo headInfo;

        if(!remoteHeads.TryGetValue(userId, out headInfo)) {
            headInfo = new RemoteHeadInfo();
            headInfo.UserID = userId;
            headInfo.HeadObject = CreateRemoteHead(userId); // GameObjectの生成

            remoteHeads.Add(userId, headInfo);      // リストに追加
        }
        return headInfo;
    }

    /// <summary>
    /// リモートユーザを表すGameObjectの新規生成
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private GameObject CreateRemoteHead(long userId) {
        GameObject newHeadObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        newHeadObj.transform.parent = transform;
        newHeadObj.transform.localScale = Vector3.one * 0.2f;
        newHeadObj.name = "remote user: " + userId;
        return newHeadObj;
    }
    #endregion

    #region User Left
    /// <summary>
    /// リモートユーザがセッションから出て行ったときのハンドラ
    /// 頭を表すGameObjectを削除し、リモートユーザのリストから削除する
    /// </summary>
    /// <param name="user"></param>
    private void UserLeftSession(User user) {
        int userId = user.GetID();  // セッションから離れたユーザのIDを取得
        if(userId != SharingStage.Instance.Manager.GetLocalUser().GetID()) {
            RemoveRemoteHead(remoteHeads[userId].HeadObject);
            remoteHeads.Remove(userId);     // リモートユーザのリストから削除する
        }
    }

    /// <summary>
    /// シーンからリモートユーザのGameObjectを消す
    /// </summary>
    /// <param name="remoteHeadObject"></param>
    private void RemoveRemoteHead(GameObject remoteHeadObject) {
        DestroyImmediate(remoteHeadObject);
    }
    #endregion

    #region Unity Life Cycle
    /// <summary>
    /// 自分の頭(カメラ)の位置をBroadCastしてもらうようサーバに依頼する
    /// </summary>
    private void Update() {
        Transform headTransform = Camera.main.transform;
        // カメラのPositionとRotationを基準点(HologramCollection)のローカル空間系に変換する
        // したがってHologramCollectionはScale(1,1,1), Rotation(0,0,0)にしておいた方がいい
        Vector3 headPosition = transform.InverseTransformPoint(headTransform.position);
        Quaternion headRotation = Quaternion.Inverse(transform.rotation) * headTransform.rotation;
        // サーバにBroadcast依頼
        EasySharingMessages.Instance.SendHeadTransform(headPosition, headRotation);
    }

    /// <summary>
    /// 最終処理
    /// </summary>
    protected override void OnDestroy() {
        if (SharingStage.Instance != null) {
            if (SharingStage.Instance.SessionUsersTracker != null) {
                SharingStage.Instance.SessionUsersTracker.UserJoined -= UserJoinedSession;
                SharingStage.Instance.SessionUsersTracker.UserLeft -= UserLeftSession;
            }
        }

        base.OnDestroy();
    }
    #endregion

    #region Receive Message from server
    /// <summary>
    /// メッセージ受信時のハンドラ
    /// </summary>
    /// <param name="msg">リモートユーザからのメッセージ、ブロードキャストされてくる</param>
    private void UpdateHeadTransform(NetworkInMessage msg) {
        long userID = msg.ReadInt64();  // ユーザIDを取得

        // メッセージ内のPosition, Rotationデータを取得
        Vector3 headPos = EasySharingMessages.Instance.ReadVector3(msg);
        Quaternion headRot = EasySharingMessages.Instance.ReadQuaternion(msg);

        // シーン上のオブジェクトに受信データを反映
        RemoteHeadInfo headInfo = GetRemoteHeadInfo(userID);
        headInfo.HeadObject.transform.localPosition = headPos;
        headInfo.HeadObject.transform.localRotation = headRot;
    }
    #endregion
}
