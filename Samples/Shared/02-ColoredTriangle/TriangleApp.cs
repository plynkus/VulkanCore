﻿using System;
using System.Threading.Tasks;

namespace VulkanCore.Samples.Triangle
{
    public class TriangleApp : VulkanApp
    {
        private RenderPass _renderPass;
        private ImageView[] _imageViews;
        private Framebuffer[] _framebuffers;
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;

        public TriangleApp(IntPtr hInstance, IWindow window) : base(hInstance, window)
        {
        }

        protected override async Task InitializePermanentAsync()
        {
            _renderPass =     ToDispose(CreateRenderPass());
            _pipelineLayout = ToDispose(CreatePipelineLayout());
        }

        protected override async Task InitializeFrameAsync()
        {
            _imageViews =   ToDispose(CreateImageViews());
            _framebuffers = ToDispose(CreateFramebuffers());
            _pipeline =     ToDispose(await CreateGraphicsPipelineAsync());
        }

        private RenderPass CreateRenderPass()
        {
            var subpasses = new[]
            {
                new SubpassDescription(new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) })
            };
            var attachments = new[]
            {
                new AttachmentDescription
                {
                    Samples = SampleCounts.Count1,
                    Format = Swapchain.Format,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.PresentSrcKhr,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare
                }
            };

            var createInfo = new RenderPassCreateInfo(subpasses, attachments);
            return Device.Logical.CreateRenderPass(createInfo);
        }

        private ImageView[] CreateImageViews()
        {
            var imageViews = new ImageView[SwapchainImages.Length];
            for (int i = 0; i < SwapchainImages.Length; i++)
            {
                imageViews[i] = SwapchainImages[i].CreateView(new ImageViewCreateInfo(
                    Swapchain.Format,
                    new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)));
            }
            return imageViews;
        }

        private Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[SwapchainImages.Length];
            for (int i = 0; i < SwapchainImages.Length; i++)
            {
                framebuffers[i] = _renderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    new[] { _imageViews[i] }, 
                    Window.Width, 
                    Window.Height));
            }
            return framebuffers;
        }

        private PipelineLayout CreatePipelineLayout()
        {
            var layoutCreateInfo = new PipelineLayoutCreateInfo();
            return Device.Logical.CreatePipelineLayout(layoutCreateInfo);
        }

        private async Task<Pipeline> CreateGraphicsPipelineAsync()
        {
            ShaderModule vertexShader =   await Content.LoadAsync<ShaderModule>("shader.vert.spv");
            ShaderModule fragmentShader = await Content.LoadAsync<ShaderModule>("shader.frag.spv");
            var shaderStageCreateInfos = new[]
            {
                new PipelineShaderStageCreateInfo(ShaderStages.Vertex, vertexShader, "main"),
                new PipelineShaderStageCreateInfo(ShaderStages.Fragment, fragmentShader, "main")
            };

            var vertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo();
            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList);
            var viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
                new Viewport(0, 0, Window.Width, Window.Height),
                new Rect2D(Offset2D.Zero, new Extent2D(Window.Width, Window.Height)));
            var rasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };
            var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            };
            var colorBlendAttachmentState = new PipelineColorBlendAttachmentState
            {
                SrcColorBlendFactor = BlendFactor.One,
                DstColorBlendFactor = BlendFactor.Zero,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = ColorComponents.All
            };
            var colorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(
                new[] { colorBlendAttachmentState });

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                _pipelineLayout, _renderPass, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                vertexInputStateCreateInfo,
                rasterizationStateCreateInfo,
                viewportState: viewportStateCreateInfo,
                multisampleState: multisampleStateCreateInfo,
                colorBlendState: colorBlendStateCreateInfo);
            return Device.Logical.CreateGraphicsPipeline(pipelineCreateInfo);
        }

        protected override void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex)
        {
            var renderPassBeginInfo = new RenderPassBeginInfo(
                _framebuffers[imageIndex],
                new Rect2D(Offset2D.Zero, new Extent2D(Window.Width, Window.Height)),
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)));

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, _pipeline);
            cmdBuffer.CmdDraw(3);
            cmdBuffer.CmdEndRenderPass();
        }
    }
}
