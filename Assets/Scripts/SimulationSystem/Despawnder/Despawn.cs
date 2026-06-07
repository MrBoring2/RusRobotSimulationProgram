using Assets.Scripts.Managers;
using Assets.Scripts.Providers.PropertyProviders;
using Assets.Scripts.SimulationSystem.PropertyProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.SimulationSystem.Despawnder
{
    public class Despawn : CellDeviceBase
    {
    
        private DespawnPropertyProvider _propertyProvider { get => gameObject.GetComponent<IPropertyProvider>() as DespawnPropertyProvider; }
        protected override void StartSim(StartProgramm s)
        {

        }

        protected override void StopSim(StopProgramm s)
        {

        }
        void UpdateVisualization()
        {
            transform.localScale = new Vector3(_propertyProvider.DetectionX, _propertyProvider.DetectionY, _propertyProvider.DetectionZ);
        }
        private void FixedUpdate()
        {
            UpdateVisualization();
        }
        private void OnCollisionEnter(UnityEngine.Collision collision)
        {
            if (!_propertyProvider.IsActive) return;
            if (SIM_STATUS != SIM_STAT.PLAY) return;
            if (collision == null) return;
            if (collision.gameObject.GetComponent<IPropertyProvider>() is WorkpiecePropertyProvider)
            {
                _sceneObjectsManager.Remove(collision.gameObject.GetComponent<IPropertyProvider>().Id, true);
            }
        }
    }
}
