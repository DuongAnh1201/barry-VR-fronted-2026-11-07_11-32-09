using System;
using System.Collections.Generic;
using Barry.Theme;
using UnityEngine;

namespace Barry.Environment
{
    [DisallowMultipleComponent]
    public sealed class EnvironmentView : MonoBehaviour
    {
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private Material skyMaterial;
        private Material floorMaterial;
        private Material platformMaterial;
        private Material planetMaterial;
        private Material ringMaterial;
        private Material previousSkybox;

        public static EnvironmentView Create(Transform parent, ThemeTokens tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            var root = new GameObject("Barry Environment");
            if (parent != null) root.transform.SetParent(parent, false);
            EnvironmentView view = root.AddComponent<EnvironmentView>();
            view.Build();
            view.Apply(tokens);
            return view;
        }

        private void OnEnable()
        {
            ThemeApplier.Changed += Apply;
            if (skyMaterial != null && ThemeApplier.Current != null) Apply(ThemeApplier.Current);
        }

        private void OnDisable()
        {
            ThemeApplier.Changed -= Apply;
        }

        public void Apply(ThemeTokens tokens)
        {
            if (tokens == null || skyMaterial == null) return;
            skyMaterial.SetColor("_SkyColor", tokens.envSky);
            skyMaterial.SetColor("_NebulaA", tokens.nebulaA);
            skyMaterial.SetColor("_NebulaB", tokens.nebulaB);
            skyMaterial.SetColor("_StarColor", tokens.starColor);
            skyMaterial.SetFloat("_StarMinAlpha", tokens.starMinAlpha);

            floorMaterial.SetColor("_FloorColor", tokens.floor);
            floorMaterial.SetColor("_GridColor", tokens.floorGrid);
            platformMaterial.SetColor("_FillColor", tokens.platformFill);
            platformMaterial.SetColor("_RimColor", tokens.platformRim);
            platformMaterial.SetColor("_DashedRingColor", tokens.platformRingDashed);
            planetMaterial.SetColor("_LightColor", tokens.planetLight);
            planetMaterial.SetColor("_MidColor", tokens.planetMid);
            planetMaterial.SetColor("_DarkColor", tokens.planetDark);
            ringMaterial.SetColor("_Color", tokens.planetRing);
        }

        private void Build()
        {
            previousSkybox = RenderSettings.skybox;
            skyMaterial = CreateMaterial("ProceduralSkybox");
            floorMaterial = CreateMaterial("FloorGrid");
            platformMaterial = CreateMaterial("PlatformDisc");
            planetMaterial = CreateMaterial("Planet");
            ringMaterial = CreateMaterial("Ring");
            RenderSettings.skybox = skyMaterial;

            CreateMeshObject("Floor", EnvironmentMeshFactory.CreateFloor(), floorMaterial,
                new Vector3(0f, 0f, 5f), Quaternion.identity, Vector3.one);
            CreateMeshObject("Operator Platform", EnvironmentMeshFactory.CreateDisc(), platformMaterial,
                new Vector3(0f, 0.005f, 0f), Quaternion.identity, Vector3.one);

            // About 40 degrees right and 25 degrees above a 1.6 m eye at 30 m.
            // The larger scale preserves the intended apparent size at that distance.
            const float planetScale = 2.5f;
            Vector3 planetPosition = new Vector3(17.5f, 14.3f, 20.8f);
            CreateMeshObject("Ringed Planet", EnvironmentMeshFactory.CreateUvSphere(), planetMaterial,
                planetPosition, Quaternion.identity, Vector3.one * planetScale);
            CreateMeshObject("Planet Ring", EnvironmentMeshFactory.CreateAnnulus(), ringMaterial,
                planetPosition, Quaternion.Euler(70f, 0f, -15f), Vector3.one * planetScale);
        }

        private Material CreateMaterial(string resourceName)
        {
            // Shaders live under Environment/Resources so Unity retains them in
            // Quest player builds; an unreferenced Shader.Find target is stripped.
            Shader shader = Resources.Load<Shader>(resourceName);
            if (shader == null) throw new InvalidOperationException("Missing environment shader resource: " + resourceName);
            var material = new Material(shader) { name = resourceName + " (Runtime)" };
            ownedMaterials.Add(material);
            return material;
        }

        private void CreateMeshObject(
            string objectName, Mesh mesh, Material material,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            ownedMeshes.Add(mesh);
            var child = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = localRotation;
            child.transform.localScale = localScale;
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            child.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void OnDestroy()
        {
            ThemeApplier.Changed -= Apply;
            if (RenderSettings.skybox == skyMaterial) RenderSettings.skybox = previousSkybox;
            for (int i = 0; i < ownedMaterials.Count; i++) Destroy(ownedMaterials[i]);
            for (int i = 0; i < ownedMeshes.Count; i++) Destroy(ownedMeshes[i]);
        }
    }
}
