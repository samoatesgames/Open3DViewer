using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    public class TextureResourceManager : IDisposable
    {
        private readonly PBRRenderEngine m_renderEngine;
        private readonly Dictionary<TextureSamplerIndex, Veldrid.TextureView> m_fallbackTextureViews
            = new Dictionary<TextureSamplerIndex, Veldrid.TextureView>();

        private readonly ConcurrentDictionary<string, Veldrid.TextureView> m_gltfTextureCache =
            new ConcurrentDictionary<string, Veldrid.TextureView>();

        public TextureResourceManager(PBRRenderEngine engine)
        {
            m_renderEngine = engine;
            LoadFallbackTextures();
        }

        public void Dispose()
        {
            foreach (var textureView in m_fallbackTextureViews.Values)
            {
                textureView.Dispose();
            }
            m_fallbackTextureViews.Clear();

            foreach (var textureView in m_gltfTextureCache.Values)
            {
                textureView.Dispose();
            }
            m_gltfTextureCache.Clear();
        }

        private string CalculateTextureHash(SharpGLTF.Schema2.Texture inputTexture, out byte[] textureBytes)
        {
            textureBytes = inputTexture.PrimaryImage.Content.Content.ToArray();
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var sb = new StringBuilder();
                foreach (var b in md5.ComputeHash(textureBytes))
                {
                    sb.Append(b.ToString("x2").ToLower());
                }
                return sb.ToString();
            }
        }

        public Veldrid.TextureView LoadTexture(SharpGLTF.Schema2.Texture inputTexture)
        {
            var textureHash = CalculateTextureHash(inputTexture, out var textureBytes);
            if (m_gltfTextureCache.TryGetValue(textureHash, out var existingTexture))
            {
                return existingTexture;
            }

            try
            {
                using (var stream = new MemoryStream(textureBytes, false))
                {
                    var texture = LoadTexture(stream);

                    if (!m_gltfTextureCache.TryAdd(textureHash, texture))
                    {
                        texture.Dispose();
                        return m_gltfTextureCache[textureHash];
                    }

                    return texture;
                }
            }
            catch
            {
                return null;
            }
        }

        public Veldrid.TextureView LoadTexture(Stream stream)
        {
            var imageSharpTexture = new Veldrid.ImageSharp.ImageSharpTexture(stream, !Debugger.IsAttached);
            var deviceTexture = imageSharpTexture.CreateDeviceTexture(m_renderEngine.GraphicsDevice, m_renderEngine.ResourceFactory);
            return CreateTextureView(deviceTexture);
        }

        public Veldrid.TextureView CreateTextureView(Veldrid.Texture inputTexture)
        {
            return m_renderEngine.ResourceFactory.CreateTextureView(inputTexture);
        }

        public Veldrid.TextureView GetFallbackTexture(TextureSamplerIndex samplerType)
        {
            if (m_fallbackTextureViews.TryGetValue(samplerType, out var fallbackView))
            {
                return fallbackView;
            }
            return m_fallbackTextureViews[TextureSamplerIndex.Diffuse];
        }

        private void LoadFallbackTextures()
        {
            using (var stream = new FileStream("Assets/Fallback Assets/DefaultDiffuseMap.png", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var defaultTextureView = LoadTexture(stream);
                defaultTextureView.Name = $"DefaultTextureView_{TextureSamplerIndex.Diffuse}";
                m_fallbackTextureViews[TextureSamplerIndex.Diffuse] = defaultTextureView;
            }

            using (var stream = new FileStream("Assets/Fallback Assets/DefaultNormalMap.png", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var defaultTextureView = LoadTexture(stream);
                defaultTextureView.Name = $"DefaultTextureView_{TextureSamplerIndex.Normal}";
                m_fallbackTextureViews[TextureSamplerIndex.Normal] = defaultTextureView;
            }
        }
    }
}
