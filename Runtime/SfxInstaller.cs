using AudioPlayerService.Runtime.Configs;
using Zenject;

namespace AudioPlayerService.Runtime
{
    public class SfxInstaller : Installer<SfxPlayerConfig,SfxInstaller>
    {
        private readonly SfxPlayerConfig _sfxPlayerConfig;

        public SfxInstaller(SfxPlayerConfig sfxPlayerConfig)
        {
            _sfxPlayerConfig = sfxPlayerConfig;
        }
        public override void InstallBindings()
        {
            Container.Bind<SfxPlayerConfig>().FromInstance(_sfxPlayerConfig).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<SfxPlayer>().AsSingle().NonLazy();
        }
    }
}