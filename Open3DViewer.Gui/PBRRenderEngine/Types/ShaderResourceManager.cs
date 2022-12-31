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

        public ShaderResourceManager(PBRRenderEngine renderEngine)
        {
            m_renderEngine = renderEngine;
            LoadShaders();
        }

        public void Dispose()
        {
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
            var objectShader = new ObjectShader();
            m_shaderCache[objectShader.GetType()] = CompileShader(objectShader);
            m_vertexLayoutCache[objectShader.GetType()] = new[] { objectShader.GetVertexLayout() };
        }

        private Shader[] CompileShader(ObjectShader shader)
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

//#if DEBUG
//        private void WatchForShaderChanges(PBRRenderEngine engine, ObjectShader shader)
//        {
//            foreach (var shaderFile in new[]
//                     {
//                         shader.GetVertexShaderPath(),
//                         shader.GetPixelShaderPath()
//                     })
//            {
//                var fileDirectory = Path.GetDirectoryName(shaderFile);
//                var file = Path.GetFileName(shaderFile);
//                var watcher = new FileSystemWatcher(fileDirectory, file)
//                {
//                    NotifyFilter = NotifyFilters.Attributes
//                                   | NotifyFilters.CreationTime
//                                   | NotifyFilters.DirectoryName
//                                   | NotifyFilters.FileName
//                                   | NotifyFilters.LastAccess
//                                   | NotifyFilters.LastWrite
//                                   | NotifyFilters.Security
//                                   | NotifyFilters.Size,
//                    EnableRaisingEvents = true
//                };
//                watcher.Changed += (sender, args) =>
//                {
//                    CreateShaders(engine.ResourceFactory, shader);
//                    m_pipeline = CreatePipeline(engine, shader);
//                };
//                m_shaderWatchers.Add(watcher);
//            }
//        }
//#endif
    }
}
