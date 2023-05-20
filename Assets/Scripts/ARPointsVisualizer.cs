using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

public class ARPointsVisualizer : MonoBehaviour
{
    [SerializeField] GameObject pointCloudRoot;
    List<Vector3> points;
    ParticleSystem m_ParticleSystem;
    ParticleSystem.Particle[] m_Particles;
    int m_NumParticles;
    bool showState = true;

    // Start is called before the first frame update
    void Start()
    {
        m_ParticleSystem = pointCloudRoot.GetComponent<ParticleSystem>();
        points = new List<Vector3>();
        LoadCloudPoint();
        RenderPoints(points);
    }

    // Update is called once per frame
    void Update()
    {
        SetVisible(showState);
    }

    public void ShowPointCloud()
    {
        
        Toggle toggle = GameObject.Find("PointShow").GetComponent<Toggle>();
        showState = toggle.isOn;
        Debug.Log($"change point show state: {showState}");
        SetVisible(showState);
    }

    void SetVisible(bool visible)
    {
        if (m_ParticleSystem == null)
            return;

        var renderer = m_ParticleSystem.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = visible;
    }

    void LoadCloudPoint()
    {
        PoseManager poseManager = FindObjectOfType<PoseManager>();
        TextAsset point3Djson = Resources.Load("points3D") as TextAsset;
        try
        {
            JObject obj = JObject.Parse(point3Djson.ToString());
            foreach (JObject point in obj["points"])
            {
                float[] t = point["pos"].ToObject<List<float>>().ToArray();
                Vector3 rightPose = new(t[0], t[1], t[2]);
                Pose leftPose = poseManager.Pose_Right2Left(new Pose(rightPose, new Quaternion(0, 0, 0, 1)));
                points.Add(leftPose.position);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }


    void SetParticlePosition(int index, Vector3 position)
    {
        m_Particles[index].startColor = m_ParticleSystem.main.startColor.color;
        m_Particles[index].startSize = m_ParticleSystem.main.startSize.constant;
        m_Particles[index].position = position;
        m_Particles[index].remainingLifetime = 1000f;
    }

    void RenderPoints(List<Vector3> points)
    {
        // Make sure we have enough particles to store all the ones we want to draw
        int numParticles = points.Count;
        if (m_Particles == null || m_Particles.Length < numParticles)
        {
            m_Particles = new ParticleSystem.Particle[numParticles];
        }
        for (int i = 0; i < numParticles; i++)
        {
            SetParticlePosition(i, points[i]);
        }

        // Remove any existing particles by setting remainingLifetime
        // to a negative value.
        for (int i = numParticles; i < m_NumParticles; ++i)
        {
            m_Particles[i].remainingLifetime = -1f;
        }

        m_ParticleSystem.SetParticles(m_Particles, Math.Max(numParticles, m_NumParticles));
        m_NumParticles = numParticles;
    }


}
