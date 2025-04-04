using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class CustomWireframe : Assets.Scripts.UI.Wireframe
    {
        public void Awake()
        {
            Renderer meshRender = GetComponent<Renderer>();
            meshRender.material.shader = Shader.Find("Unlit/AlphaSelfIllum");
            meshRender.material.color = Color.green;
        }
    }
}
