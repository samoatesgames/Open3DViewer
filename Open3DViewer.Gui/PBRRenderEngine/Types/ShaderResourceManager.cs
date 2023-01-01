using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    public class ShaderResourceManager : IDisposable
    {
        private readonly PBRRenderEngine m_renderEngine;
        private readonly Dictionary<Type, Shader[]> m_shaderCache = new Dictionary<Type, Shader[]>();
        private readonly Dictionary<Type, VertexLayoutDescription[]> m_vertexLayoutCache = new Dictionary<Type, VertexLayoutDescription[]>();

#if DEBUG
        public delegate void ShaderReloaded(IRenderShader shader);
        public event ShaderReloaded OnShaderReloaded;

        private readonly List<FileSystemWatcher> m_shaderWatchers = new List<FileSystemWatcher>();
#endif

        public ShaderResourceManager(PBRRenderEngine renderEngine)
        {
            m_renderEngine = renderEngine;
            LoadShaders();
        }

        public void Dispose()
        {
#if DEBUG
            foreach (var watcher in m_shaderWatchers)
            {
                watcher.Dispose();
            }
            m_shaderWatchers.Clear();
#endif

            foreach (var shaders in m_shaderCache.Values)
            {
                foreach (var shader in shaders)
                {
                    shader.Dispose();
                }
            }
            m_shaderCache.Clear();
        }

        public ShaderSetDescription GetShaderSet<TShader>() where TShader : IRenderShader
        {
            return new ShaderSetDescription
            (
                vertexLayouts: m_vertexLayoutCache[typeof(TShader)],
                shaders: m_shaderCache[typeof(TShader)]
            );
        }

        private void LoadShaders()
        {
            foreach (var shader in new[]
                     {
                         new ObjectShader()
                     })
            {
                LoadShader(shader);
#if DEBUG
                WatchForShaderChanges(shader);
#endif
            }
        }

        private void LoadShader(IRenderShader shader)
        {
            try
            {
                var compiledShaders = CompileShader(shader);
                var vertexLayouts = new[] { shader.GetVertexLayout() };
                m_shaderCache[shader.GetType()] = compiledShaders;
                m_vertexLayoutCache[shader.GetType()] = vertexLayouts;
            }
            catch (Exception ex)
            {
                // TODO: Log out a shader compile issue
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

        }

        private Shader[] CompileShader(IRenderShader shader)
        {
            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                ShaderFileToBytes(shader.GetVertexShaderPath()),
                "main");
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                ShaderFileToBytes(shader.GetPixelShaderPath()),
                "main");

            return m_renderEngine.ResourceFactory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        }

        private byte[] ShaderFileToBytes(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    return Encoding.UTF8.GetBytes(reader.ReadToEnd());
                }
            }
        }

#if DEBUG
        private void WatchForShaderChanges(IRenderShader shader)
        {
            foreach (var shaderFile in new[]
                     {
                         shader.GetVertexShaderPath(),
                         shader.GetPixelShaderPath()
                     })
            {
                var fileDirectory = Path.GetDirectoryName(shaderFile);
                if (fileDirectory == null)
                {
                    continue;
                }

                var file = Path.GetFileName(shaderFile);
                var watcher = new FileSystemWatcher(fileDirectory, file)
                {
                    NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                watcher.Changed += (sender, args) =>
                {
                    LoadShader(shader);
                    OnShaderReloaded?.Invoke(shader);
                };
                m_shaderWatchers.Add(watcher);
            }
        }
#endif
    }
}
