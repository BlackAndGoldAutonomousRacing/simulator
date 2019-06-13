/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using UnityEngine;

namespace Simulator.Sensors
{
    [SensorType("GPS Device", new[] { typeof(GpsData) })]
    public class GpsSensor : SensorBase
    {
        [SensorParameter]
        public float Frequency = 12.5f;

        [SensorParameter]
        public bool IgnoreMapOrigin = false;

        float NextSend;
        uint SendSequence;

        IBridge Bridge;
        IWriter<GpsData> Writer;

        MapOrigin MapOrigin;

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<GpsData>(Topic);
        }

        public void Start()
        {
            MapOrigin = MapOrigin.Find();

            NextSend = Time.time + 1.0f / Frequency;
        }

        void Update()
        {
            if (MapOrigin == null || Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            if (Time.time < NextSend)
            {
                return;
            }
            NextSend = Time.time + 1.0f / Frequency;

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            Writer.Write(new GpsData()
            {
                Name = Name,
                Frame = Frame,
                Time = SimulatorManager.Instance.CurrentTime,
                Sequence = SendSequence++,

                IgnoreMapOrigin = IgnoreMapOrigin,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Altitude = location.Altitude,
                Northing = location.Northing,
                Easting = location.Easting,
                Orientation = transform.rotation,
            });
        }

        public Api.Commands.GpsData GetData()
        {
            var location = MapOrigin.GetGpsLocation(transform.position);

            var data = new Api.Commands.GpsData
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Easting = location.Altitude,
                Northing = location.Northing,
                Altitude = location.Easting,
                Orientation = -transform.rotation.eulerAngles.y
            };
            return data;
        }
    }
}
