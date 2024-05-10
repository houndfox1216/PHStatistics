using System;
using System.Framework.AzureBlobStorage;
using System.IO;

namespace EmptyProject.Services.Storage {
    /// <summary>
    /// Azure Blob Service
    /// </summary>
    public class StorageContext : AzureBlobStorageContext {
        /// <summary>
        /// 建構 StorageContext
        /// </summary>
        public StorageContext() : base() { }

        public override void InitializeStorage() {
            #region Initialize Temporary Container

            GetContainer("temporary");
            ClearTemporary(TimeSpan.FromDays(1));

            #endregion

            #region Initialize admin Container

            GetContainer("admin");

            if (GetBlob("admin", "resources/cloudfun.svg") is Uri cloudfunSvg && !Exists(cloudfunSvg)) {
                var file = new FileInfo("./wwwroot/resources/cloudfun.svg");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/svg+xml", "admin", "resources/cloudfun.svg");
                }
            }
            if (GetBlob("admin", "resources/cloudfun.png") is Uri cloudfunPng && !Exists(cloudfunPng)) {
                var file = new FileInfo("./wwwroot/resources/cloudfun.png");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/cloudfun.png");
                }
            }
            if (GetBlob("admin", "resources/admin.svg") is Uri adminSvg && !Exists(adminSvg)) {
                var file = new FileInfo("./wwwroot/resources/admin.svg");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/svg+xml", "admin", "resources/admin.svg");
                }
            }
            if (GetBlob("admin", "resources/admin.png") is Uri adminPng && !Exists(adminPng)) {
                var file = new FileInfo("./wwwroot/resources/admin.png");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/admin.png");
                }
            }
            if (GetBlob("admin", "resources/operator.svg") is Uri operatorSvg && !Exists(operatorSvg)) {
                var file = new FileInfo("./wwwroot/resources/operator.svg");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/svg+xml", "admin", "resources/operator.svg");
                }
            }
            if (GetBlob("admin", "resources/operator.png") is Uri operatorPng && !Exists(operatorPng)) {
                var file = new FileInfo("./wwwroot/resources/operator.png");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/operator.png");
                }
            }
            if (GetBlob("admin", "resources/man.svg") is Uri manSvg && !Exists(manSvg)) {
                var file = new FileInfo("./wwwroot/resources/man.svg");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/svg+xml", "admin", "resources/man.svg");
                }
            }
            if (GetBlob("admin", "resources/man.png") is Uri manPng && !Exists(manPng)) {
                var file = new FileInfo("./wwwroot/resources/man.png");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/man.png");
                }
            }
            if (GetBlob("admin", "resources/woman.svg") is Uri womanSvg && !Exists(womanSvg)) {
                var file = new FileInfo("./wwwroot/resources/woman.svg");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/svg+xml", "admin", "resources/woman.svg");
                }
            }
            if (GetBlob("admin", "resources/woman.png") is Uri womanPng && !Exists(womanPng)) {
                var file = new FileInfo("./wwwroot/resources/woman.png");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/woman.png");
                }
            }
            if (GetBlob("admin", "resources/no-image.svg") is Uri noImageSvg && !Exists(noImageSvg)) {
                var file = new FileInfo("./wwwroot/resources/no-image.svg");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/no-image.svg");
                }
            }
            if (GetBlob("admin", "resources/no-image.png") is Uri noImagePng && !Exists(noImagePng)) {
                var file = new FileInfo("./wwwroot/resources/no-image.png");
                if (file.Exists) {
                    using var stream = file.OpenRead();
                    Create(stream, "image/png", "admin", "resources/no-image.png");
                }
            }

            #endregion
        }
    }
}
