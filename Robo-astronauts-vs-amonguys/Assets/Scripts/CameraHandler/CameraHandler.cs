﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using GameManagement;

public class CameraHandler : MonoBehaviour
{
    [SerializeField] float minOffsetY = 0;
    [SerializeField] float maxOffsetY = 15f;
    CinemachineTransposer transposer;
    [SerializeField] float turnSpeed = 1f;
    float mouse_sensitivity;
    bool invert_y = true;
    void Start()
    {
        transposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>();
        mouse_sensitivity = GameSettings.GetMouseSensitivity();
    }

    private void LateUpdate()
    {
        float vertical = Input.GetAxis("Mouse Y") * turnSpeed * mouse_sensitivity;
        vertical *= (invert_y ? -1f : 1f);
        transposer.m_FollowOffset.y += vertical;
        float offsetY = transposer.m_FollowOffset.y;
        transposer.m_FollowOffset.y = Mathf.Clamp(offsetY, minOffsetY, maxOffsetY);
    }
}
