using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OWSBG.Tests
{
    /// <summary>The look's render features (PRG-03/04): shaders compile, the renderer carries both full-screen passes.</summary>
    public class RenderSetupTests
    {
        const string RendererPath = "Assets/_Project/Settings/Rendering/URP_Renderer.asset";

        [Test]
        public void ShadersExistAndAreSupported()
        {
            foreach (var name in new[] { "OWSBG/InkSprite", "OWSBG/FullScreen/PaperGrain", "OWSBG/FullScreen/ForegroundBlur" })
            {
                var shader = Shader.Find(name);
                Assert.IsNotNull(shader, name + " is missing");
                Assert.IsTrue(shader.isSupported, name + " does not compile for this platform");
            }
        }

        [Test]
        public void RendererHasForegroundBlurThenPaperGrain()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            Assert.IsNotNull(data, "URP renderer asset");
            FullScreenPassRendererFeature blur = null, grain = null;
            foreach (var f in data.rendererFeatures)
            {
                if (f is FullScreenPassRendererFeature fs && fs.name == "ForegroundBlur") blur = fs;
                if (f is FullScreenPassRendererFeature fg && fg.name == "PaperGrain") grain = fg;
            }
            Assert.IsNotNull(blur, "ForegroundBlur feature on the renderer");
            Assert.IsNotNull(grain, "PaperGrain feature on the renderer");
            Assert.IsNotNull(blur.passMaterial); Assert.IsNotNull(grain.passMaterial);
            Assert.AreEqual("OWSBG/FullScreen/ForegroundBlur", blur.passMaterial.shader.name);
            Assert.AreEqual("OWSBG/FullScreen/PaperGrain", grain.passMaterial.shader.name);
            Assert.AreEqual(FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing, blur.injectionPoint, "blur runs before bloom and tonemapping");
            Assert.AreEqual(ScriptableRenderPassInput.Depth, blur.requirements & ScriptableRenderPassInput.Depth, "blur reads depth");
            Assert.AreEqual(FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing, grain.injectionPoint, "grain sits on top of everything");
            Assert.IsTrue(blur.fetchColorBuffer && grain.fetchColorBuffer);
            Assert.AreEqual(18f, blur.passMaterial.GetFloat("_FocusDistance"), 0.01f, "focus on the gameplay plane, 18 units from the camera");
        }
    }
}
