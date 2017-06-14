using Microsoft.ProjectOxford.Vision;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebApplication1
{
    public partial class _default : System.Web.UI.Page
    {
        const string ComputerVisionKey = "", 
            endpoint = "";
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            var filename = System.IO.Path.GetFileName(this.FileUpload1.PostedFile.FileName);
            var path = Request.MapPath("/_pic/" + filename);
            this.FileUpload1.PostedFile.SaveAs(path);
            //丟給Computer Vision API分析圖片
            var AnalysisREsult = AnalysisImage(path);
            if (AnalysisREsult == null)
            {  
                this.Label1.Text = "您上傳的資料，沒找著任何可識別的東西~";
                return;
            }

            //如果找到人臉，把處理過的照片回傳
            if (AnalysisREsult.isFaceFound)
            {
                var NewImageURL = $"http://{Request.Url.Host}{(Request.Url.Port == 80 ? "" : ":" + Request.Url.Port) }/" + AnalysisREsult.NewImageURL;
                var msg = $"您上傳的圖片，{AnalysisREsult.FaceDescription }" +
                      "<br>圖片說明 : " + AnalysisREsult.PictureDescription +
                     $" <br>識別後的圖片位於 : <a href='{ NewImageURL }' target=_blank>{  NewImageURL }</a>";
                this.Image1.ImageUrl = NewImageURL;
                this.Label1.Text = msg;
            }
        }


        public class AnalysisImageResult
        {
            public bool isFaceFound { get; set; }
            public string FaceDescription { get; set; }
            public string PictureDescription { get; set; }
            public string NewImageURL { get; set; }
        }

        /// <summary>
        /// Demo用的圖片分析
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private AnalysisImageResult  AnalysisImage(string filePath)
        {
            //回傳物件
            var AnalysisImageResult = new AnalysisImageResult();
            try
            {
                //取得原始檔案讀入BPM
                var fs2 = new FileStream(filePath, FileMode.Open);
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fs2);
                Graphics g = Graphics.FromImage(bmp);
                fs2.Close();
                //辨識
                var visionClient = new Microsoft.ProjectOxford.Vision.VisionServiceClient(
                    ComputerVisionKey, endpoint);
                using (var fs = new FileStream(filePath, FileMode.Open))
                {
                    var Results = visionClient.AnalyzeImageAsync(
                        fs, new VisualFeature[] { VisualFeature.Faces, VisualFeature.Description }).Result;

                    int resizeFactor = 1;

                    int isM = 0, isF = 0;
                    //如果找到臉，就畫
                    foreach (var item in Results.Faces)
                    {
                        var faceRect = item.FaceRectangle;
                        //畫框
                        g.DrawRectangle(
                                    new Pen(Brushes.Red, 10),
                                    new Rectangle(
                                        faceRect.Left * resizeFactor,
                                        faceRect.Top * resizeFactor,
                                        faceRect.Width * resizeFactor,
                                        faceRect.Height * resizeFactor
                                        ));
                        //顯示年紀
                        var age = 0;
                        if (item.Gender.StartsWith("F")) age = item.Age - 2; else age = item.Age;
                        g.DrawString(age.ToString(), new Font("Arial", 16),
                            new SolidBrush(Color.Black),
                            faceRect.Left * resizeFactor + 3, faceRect.Top * resizeFactor + 3);
                        //紀錄性別
                        if (item.Gender.StartsWith("M"))
                            isM += 1;
                        else
                            isF += 1;
                    }
                    //顯示分析結果

                    AnalysisImageResult.PictureDescription = Results.Description.Captions[0].Text;
                    //如果update了照片，則顯示新圖
                    if (Results.Faces.Count() > 0)
                    {
                        AnalysisImageResult.FaceDescription = String.Format("找到{0}張臉, {1}男 {2}女", Results.Faces.Count(), isM, isF);
                        AnalysisImageResult.isFaceFound = true;
                        var filename = Guid.NewGuid() + System.IO.Path.GetExtension(filePath);
                        bmp.Save(System.Web.HttpContext.Current.Request.MapPath("/_pic/" + filename));
                        AnalysisImageResult.NewImageURL = "/_pic/" + filename;
                    }
                    return AnalysisImageResult;
                }
            }
            catch (Exception ex)
            {
                //process exception
                throw ex;
            }
        }
    }
}