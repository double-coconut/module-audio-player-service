using System;
using UnityEngine;

namespace AudioPlayerService.Runtime
{
    internal class PlayingSfx : IDisposable
    {
        public AudioSource Source { get; }
        private IDisposable _disposable;

        public PlayingSfx(AudioSource source, IDisposable disposable)
        {
            Source = source;
            _disposable = disposable;
        }


        public void Dispose()
        {
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}