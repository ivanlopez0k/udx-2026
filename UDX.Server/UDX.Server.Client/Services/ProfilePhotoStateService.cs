using System;

namespace UDX.Server.Client.Services
{
    public class ProfilePhotoStateService
    {
        private string _fotoPerfilUrl = "";

        public string FotoPerfilUrl
        {
            get => _fotoPerfilUrl;
            set
            {
                if (_fotoPerfilUrl != value)
                {
                    _fotoPerfilUrl = value;
                    NotifyStateChanged();
                }
            }
        }

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();

        public void LoadFromLocalStorage(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url) && url != _fotoPerfilUrl)
            {
                _fotoPerfilUrl = url;
                NotifyStateChanged();
            }
        }

        public void Clear()
        {
            if (!string.IsNullOrEmpty(_fotoPerfilUrl))
            {
                _fotoPerfilUrl = "";
                NotifyStateChanged();
            }
        }
    }
}