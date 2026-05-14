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
public class Spawner : CellDeviceBase
{
    public SpawnPropertyProvider spawnPointPropertyProvider;
    private float timeOldSpawn = 0f;
    private void Start()
    {
        base.Start();
        spawnPointPropertyProvider = gameObject.GetComponent<SpawnPropertyProvider>();
    }
    private void FixedUpdate()
    {
        if (SIM_STATUS != SIM_STAT.PLAY) return;
        if (spawnPointPropertyProvider.spawnClick)
        {
            SpawnItemOnConveyor();
            spawnPointPropertyProvider.spawnClick = false;
        }
        if(_SimManager.GetTimeSimulationFloat() - timeOldSpawn >= spawnPointPropertyProvider.spawnInterval && spawnPointPropertyProvider.spawnInterval != 0 && spawnPointPropertyProvider.spawnOn)
        {
            SpawnItemOnConveyor();
            timeOldSpawn = _SimManager.GetTimeSimulationFloat();
        }
    }
    void SpawnItemOnConveyor()
    {
        if (!string.IsNullOrEmpty(spawnPointPropertyProvider.DetailName))
        {
            GameObject newItem = Instantiate(spawnPointPropertyProvider.detailsList.Where(x=>x.Name == spawnPointPropertyProvider.DetailName).FirstOrDefault().itemPrefab, transform.position, Quaternion.Euler(spawnPointPropertyProvider.RotateSpawnDetail));
            Rigidbody rb = newItem.GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }
    }
    protected override void StopSim(StopProgramm s)
    {

    }
    protected override void StartSim(StartProgramm s)
    {
    }
}