using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Sharing;
using UnityEngine;

public class EasySharingCaracterManager : MonoBehaviour {


    public enum CharacterType : byte {
        Unity_Chan,
        Nanica_Chan,
        None
    }
    public CharacterType selfCharacterType;
    private int selfCharacterTypeInt;

    // public GameObject unityChan;
    // public GameObject nanicaChan;

    // private Dictionary<CharacterType, GameObject> prefabsDict = new Dictionary<CharacterType, GameObject>();

    /// <summary>
    /// キャラクターの辞書
    /// </summary>
    public List<GameObject> prefabs = new List<GameObject>();
    /// <summary>
    /// リモートユーザのキャラクターを表す内部クラス
    /// </summary>
    public class RemoteCharacterInfo {
        public long UserID;
        public CharacterType characterType;
        public GameObject characterObject;
    }

    /// <summary>
    /// リモートユーザの辞書
    /// </summary>
    private Dictionary<long, RemoteCharacterInfo> remoteCharacters = new Dictionary<long, RemoteCharacterInfo>();

    /// <summary>
    /// 自機
    /// </summary>
    GameObject selfCharacter;

    /// <summary>
    /// 初期処理
    /// 自機の生成はここで行う
    /// </summary>
    private void Start() {
        // 受信メッセージハンドラの設定
        EasySharingMessages.Instance.MessageHandlers[EasySharingMessages.TestMessageID.CharacterTransform] = UpdateRemoteCharacterTransform;
        
        // 辞書の作成
        // TODO: ダサいのであとで直す
        //prefabsDict.Add(CharacterType.Unity_Chan, unityChan);
        //prefabsDict.Add(CharacterType.Nanica_Chan, nanicaChan);

        // 自機の生成
        switch(selfCharacterType) {
            case CharacterType.Unity_Chan:
                selfCharacterTypeInt = 0;
                break;
            case CharacterType.Nanica_Chan:
                selfCharacterTypeInt = 1;
                break;
            case CharacterType.None:
                selfCharacterTypeInt = 100;
                break;
        }

        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f),0,UnityEngine.Random.Range(-0.5f, 0.5f) + 2.0f);
        if (selfCharacterTypeInt != 100) {
            selfCharacter = Instantiate(prefabs[selfCharacterTypeInt], spawnPosition, Quaternion.identity);
            selfCharacter.transform.parent = transform;
        }

        // セッション出入り関連の初期化
        if(SharingStage.Instance.IsConnected) {
            Connected();
        } else {
            SharingStage.Instance.SharingManagerConnected += Connected;
        }
    }

    private void Connected(object sender = null, EventArgs e = null) {
        SharingStage.Instance.SharingManagerConnected -= Connected;

        SharingStage.Instance.SessionUsersTracker.UserJoined += UserJoinedSession;
        SharingStage.Instance.SessionUsersTracker.UserLeft += UserLeftSession;
    }

    private void Update() {
        // 自機のTransformデータを送信する
        Transform selfTransform = selfCharacter.transform;

        Vector3 characterPosition = transform.InverseTransformPoint(selfTransform.position);
        Quaternion characterRotation = Quaternion.Inverse(transform.rotation) * selfTransform.rotation;
        // サーバにBroadCast
        EasySharingMessages.Instance.SendSelfCharacterTransform(
            characterPosition, 
            characterRotation, 
            selfTransform.localScale, 
            selfCharacterTypeInt);
    }

    #region User Joined
    /// <summary>
    /// リモートユーザがセッションに入ったときのハンドラ
    /// リストに入れるオブジェクトを生成する
    /// </summary>
    /// <param name="user"></param>
    private void UserJoinedSession(User user) {
        int userId = user.GetID();
        if(userId != SharingStage.Instance.Manager.GetLocalUser().GetID()) {
            GetRemoteCharacterInfo(userId);
        }
    }

    /// <summary>
    /// リストに入れるオブジェクトの生成
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private RemoteCharacterInfo GetRemoteCharacterInfo(long userId) {
        RemoteCharacterInfo characterInfo;
        if(!remoteCharacters.TryGetValue(userId, out characterInfo)) {
            characterInfo = new RemoteCharacterInfo();
            characterInfo.UserID = userId;
            characterInfo.characterType = CharacterType.None;

            remoteCharacters.Add(userId, characterInfo);
        }
        return characterInfo;
    }
    #endregion

    #region User Left
    /// <summary>
    /// ユーザがセッションから出て行ったときのハンドラ
    /// </summary>
    /// <param name="user"></param>
    private void UserLeftSession(User user) {
        int userId = user.GetID();
        if(userId != SharingStage.Instance.Manager.GetLocalUser().GetID()) {
            RemoveRemoteCharacter(remoteCharacters[userId].characterObject);
            remoteCharacters.Remove(userId);
        }
    }

    /// <summary>
    /// リモートユーザをシーンから消し去る
    /// </summary>
    /// <param name="characterObject"></param>
    private void RemoveRemoteCharacter(GameObject characterObject) {
        DestroyImmediate(characterObject);
    }
    #endregion

    /// <summary>
    /// リモートユーザから受信したメッセージのハンドラ
    /// リモートユーザキャラクターのTransformをUpdateする
    /// </summary>
    /// <param name="msg"></param>
    private void UpdateRemoteCharacterTransform(NetworkInMessage msg) {
        long userID = msg.ReadInt64();      // ユーザIDを取得
        // Transformを取得
        Vector3 remoteCharaPos = EasySharingMessages.Instance.ReadVector3(msg);
        Quaternion remoteCharaRot = EasySharingMessages.Instance.ReadQuaternion(msg);
        Vector3 remoteCharaScale = EasySharingMessages.Instance.ReadVector3(msg);
        // キャラクタータイプを取得
        int remoteCharTypeInt = msg.ReadInt32() ;
        // データ反映
        RemoteCharacterInfo remoteCharaInfo = GetRemoteCharacterInfo(userID);
        if (remoteCharaInfo.characterType == CharacterType.None) {
            
            remoteCharaInfo.characterType = remoteCharTypeInt == 0 ? CharacterType.Unity_Chan : CharacterType.Nanica_Chan;
            remoteCharaInfo.characterObject = Instantiate(prefabs[remoteCharTypeInt]);
            remoteCharaInfo.characterObject.transform.parent = transform;
            remoteCharaInfo.characterObject.name = "remote player : " + userID;
            // remotePlayerのTransformControllerはけす
            remoteCharaInfo.characterObject.GetComponent<TransformControllerActivater>().enabled = false;
        }
        // Transformの設定
        remoteCharaInfo.characterObject.transform.localPosition = remoteCharaPos;
        remoteCharaInfo.characterObject.transform.localRotation = remoteCharaRot;
        remoteCharaInfo.characterObject.transform.localScale = remoteCharaScale;
    }
}
