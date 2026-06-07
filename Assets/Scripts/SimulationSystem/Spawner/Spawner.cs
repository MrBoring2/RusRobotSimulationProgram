using Assets.Scripts.Models;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Assets.Scripts.Providers;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.CustomServiceManager;
public class Spawner : CellDeviceBase
{
    public SpawnPropertyProvider spawnPointPropertyProvider;
    public GameObject detailView;
    private SceneObjectsManager _sceneObjectsManager;
    private float timeOldSpawn = 0f;
    string oldDetailName = "";
    private void Start()
    {
        base.Start();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        spawnPointPropertyProvider = gameObject.GetComponent<SpawnPropertyProvider>();
    }
    private void FixedUpdate()
    {
        if(!string.Equals(oldDetailName, spawnPointPropertyProvider.DetailName))
        {
            UpdatePreviewMesh();
            oldDetailName = spawnPointPropertyProvider.DetailName;
        }
        if (SIM_STATUS != SIM_STAT.PLAY) return;
        if (spawnPointPropertyProvider.SpawnClick)
        {
            SpawnItem();
            spawnPointPropertyProvider.SpawnClick = false;
        }
        if(_SimManager.GetTimeSimulationFloat() - timeOldSpawn >= spawnPointPropertyProvider.SpawnInterval && spawnPointPropertyProvider.SpawnInterval != 0 && spawnPointPropertyProvider.SpawnOn)
        {
            SpawnItem();
            timeOldSpawn = _SimManager.GetTimeSimulationFloat();
        }
    }
    void SpawnItem()
    {
        if (!string.IsNullOrEmpty(spawnPointPropertyProvider.DetailName))
        {
            _sceneObjectsManager.Create(spawnPointPropertyProvider.detailsList.FirstOrDefault(x => x.Name == spawnPointPropertyProvider.DetailName).itemPrefab, transform.position, Quaternion.Euler(spawnPointPropertyProvider.RotateSpawnDetail), ObjectType.Workpiece); 
        }
    }
    private void UpdatePreviewMesh()
    {
        if (detailView == null)
        {
            Debug.LogError("detailView не назначен на спавнере!");
            return;
        }

        var meshFilter = detailView.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("У detailView отсутствует MeshFilter!");
            return;
        }

        // Находим нужный префаб
        var detailData = spawnPointPropertyProvider.detailsList
            .FirstOrDefault(x => x.Name == spawnPointPropertyProvider.DetailName);

        if (detailData?.itemPrefab == null)
        {
            Debug.LogWarning($"Не найден префаб для детали: {spawnPointPropertyProvider.DetailName}");
            meshFilter.mesh = null;
            return;
        }

        // Получаем меш
        Mesh sourceMesh = detailData.itemPrefab.GetComponent<MeshFilter>()?.sharedMesh;
        if (sourceMesh == null)
        {
            Debug.LogWarning($"У префаба {detailData.itemPrefab.name} нет MeshFilter или меша.");
            return;
        }

        // Применяем меш
        meshFilter.mesh = Instantiate(sourceMesh);

        // === КОПИРУЕМ РАЗМЕР (scale) С ПРЕФАБА ===
        detailView.transform.localScale = detailData.itemPrefab.transform.localScale;

        // Обновляем материалы
        var renderer = detailView.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            var sourceRenderer = detailData.itemPrefab.GetComponent<MeshRenderer>();
            if (sourceRenderer != null && sourceRenderer.sharedMaterials.Length > 0)
            {
                renderer.materials = (Material[])sourceRenderer.sharedMaterials.Clone();
            }
        }

        // Центрируем превью (чтобы объект отображался от центра)
        detailView.transform.localPosition = Vector3.zero;
        detailView.transform.localRotation = Quaternion.identity;
    }
    protected override void StopSim(StopProgramm s)
    {

    }
    protected override void StartSim(StartProgramm s)
    {
    }
}