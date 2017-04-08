using HoloToolkit.Unity;
using System;
using UnityEngine;

public class RefCoordinatesAnchor : MonoBehaviour {
    private Guid anchorID;
	void Start () {
        anchorID = Guid.NewGuid();
        WorldAnchorManager.Instance.AttachAnchor(gameObject, anchorID.ToString());
	}

    private void OnDestroy() {
        WorldAnchorManager.Instance.RemoveAnchor(gameObject);
    }
}
