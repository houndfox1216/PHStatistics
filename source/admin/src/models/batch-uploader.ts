// @ts-nocheck

import CloudFun, { ModelState } from "@cloudfun/core";
import { Module } from "vuex";
import BatchUploader, { UploadFile } from "@cloudfun/batch-uploader";

const uploader = new BatchUploader();
uploader.setCallback("success", (file?: UploadFile) => {
  CloudFun.send("uploader:success", file ? JSON.stringify(file) : "");
});
uploader.setCallback("completed", (file?: UploadFile) => {
  CloudFun.send("uploader:completed", file ? JSON.stringify(file) : "");
});
uploader.start();

const module: Module<BatchUploader, ModelState> = {
  namespaced: true,
  state: uploader,
}

export default module;
