using System;
using UnityEngine;

namespace Services.AudioPlayer
{
    internal struct PlaySfxEvent
    {
        public string Sound;
        public bool Overlaid;
        public Action OnEnd;
        public Vector3? WorldPos;
    }
}