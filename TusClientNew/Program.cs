using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TusClientNew
{
    class Program
    {


        private static string ServerURL { get; set; } = "https://tus-poc.kira.tools:10101/upload";
        private static string A500_MB_FILE = "C:\\Users\\dhava\\Downloads\\150MB_Contract.pdf";
        private static string A300_MB_FILE = "C:\\Users\\dhava\\Downloads\\300MB_Contract.pdf";
        private static string A200_MB_FILE = "C:\\Users\\dhava\\Downloads\\200MB_Contract.pdf";
        private static string A150_MB_FILE = "C:\\Users\\dhava\\Downloads\\150MB_Contract.pdf";

        static void Main(string[] args)
        {

            //ServerInfo();

            //UploadExampleMinimal();
            //UploadExampleStream();
            
            //UploadWithProgress();
            //UploadConnectionInterrupted();
            CancelResumeExample();




            Console.WriteLine("Press the any key");
            Console.ReadKey();
        }

        private static void UploadExampleMinimal()
        {
            var testfile = GenFileText(sizeInMb: 32);

            TusClient.TusClient tc = new TusClient.TusClient();
            tc.Uploading += (long bytesTransferred, long bytesTotal) =>
            {
                decimal perc = (decimal)(bytesTransferred / (double)bytesTotal * 100.0);
                Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal);
            };

            var fileURL = tc.Create(ServerURL, testfile);
            tc.Upload(fileURL, testfile);

            tc.Delete(fileURL);

            // Cleanup
            //System.IO.File.Delete(testfile.FullName);
        }

        


        private static void UploadExampleStream()
        {
            var testfile = GenFileText(sizeInMb: 32);

            Dictionary<string, string> metadata = new Dictionary<string, string>();
            metadata["filena"] = testfile.Name;

            TusClient.TusClient tc = new TusClient.TusClient();
            tc.Uploading += (long bytesTransferred, long bytesTotal) =>
            {
                decimal perc = (Decimal)(bytesTransferred / (double)bytesTotal * 100.0);
                Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal);
            };

            var fileURL = tc.Create(ServerURL, testfile.Length, metadata: metadata);
            using (System.IO.FileStream fs = new System.IO.FileStream(testfile.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                tc.Upload(fileURL, fs);
            }


            tc.Delete(fileURL);

            // Cleanup
            //System.IO.File.Delete(testfile.FullName);
        }


       

        private static void CancelResumeExample()
        {
            var testfile = GenFileText(sizeInMb: 32);

            int lastperc = 0;

            TusClient.TusClient tc = new TusClient.TusClient();
            tc.Uploading += (long bytesTransferred, long bytesTotal) =>
            {
                decimal perc = (decimal)(bytesTransferred / (double)bytesTotal * 100.0);
                if (perc - lastperc > 1)
                {
                    Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal);
                    lastperc = (int)perc;
                }
                if (perc > 50)
                    tc.Cancel();
            };

            var fileURL = tc.Create(ServerURL, testfile);
            try
            {
                tc.Upload(fileURL, testfile);
            }
            catch (TusClient.TusException ex)
            {
                if (ex.Status == System.Net.WebExceptionStatus.RequestCanceled)
                    Console.WriteLine("Upload Cancelled");
                else
                    throw;
            }

            System.Threading.Thread.Sleep(2000);

            tc = new TusClient.TusClient(); // Have to create new client to resume with same URL
            tc.Uploading += (long bytesTransferred, long bytesTotal) =>
            {
                decimal perc = (decimal)(bytesTransferred / (double)bytesTotal * 100.0);
                if (perc - lastperc > 1)
                {
                    Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, bytesTransferred, bytesTotal);
                    lastperc = (int)perc;
                }
            };

            Console.WriteLine("Upload Resumed");
            tc.Upload(fileURL, testfile);

            Console.WriteLine("Upload Complete");
            //tc.Delete(fileURL);

            // Cleanup
            //System.IO.File.Delete(testfile.FullName);
        }

        private static void UploadWithProgress()
        {
            var testfile = GenFileText(sizeInMb: 32);

            Stopwatch sw = new Stopwatch();
            long bytesTransferredLast = 0;
            decimal transferRate = 0;

            decimal PreviousPercentage = 0;

            TusClient.TusClient tc = new TusClient.TusClient();
            tc.Uploading += (long bytesTransferred, long bytesTotal) =>
            {
                if (sw.Elapsed.TotalSeconds > 0)
                    transferRate = (decimal)((bytesTransferred - bytesTransferredLast) / sw.Elapsed.TotalSeconds);

                decimal perc = (decimal)(bytesTransferred / (double)bytesTotal * 100.0);
                perc = Math.Truncate(perc);

                if (perc != PreviousPercentage)
                {
                    Console.WriteLine("Up {0:0.00}% {1} of {2} @ {3}/second", perc, HumanizeBytes(bytesTransferred), HumanizeBytes(bytesTotal), HumanizeBytes((long)transferRate));
                    PreviousPercentage = perc;
                }

                if (sw.Elapsed.TotalSeconds > 1)
                {
                    bytesTransferredLast = bytesTransferred;
                    sw.Restart();
                }
            };



            var fileURL = tc.Create(ServerURL, testfile);
            sw.Start();
            tc.Upload(fileURL, testfile);
            sw.Stop();

            //VerifyUpload(fileURL, testfile);

            var serverInfo = tc.getServerInfo(ServerURL);

            //if (serverInfo.SupportsDelete)
            //{
            //    if (tc.Delete(fileURL))
            //        Console.WriteLine("Upload Terminated");
            //    else
            //        Console.WriteLine("Upload Terminated FAILED");
            //}


            // Cleanup
            //System.IO.File.Delete(testfile.FullName);
        }

        private static void UploadConnectionInterrupted()
        {
            var testfile = GenFileBinary(sizeInMb: 64);

            decimal PreviousPercentage = 0;
            decimal PreviousPercentageDisconnect = 0;

            TusClient.TusClient tc = new TusClient.TusClient();
            tc.Uploading += (long bytesTransferred, long bytesTotal) =>
            {
                decimal perc = (decimal)(bytesTransferred / (double)bytesTotal * 100.0);
                perc = Math.Truncate(perc);
                if (perc != PreviousPercentage)
                {
                    Console.WriteLine("Up {0:0.00}% {1} of {2}", perc, HumanizeBytes(bytesTransferred), HumanizeBytes(bytesTotal));
                    PreviousPercentage = perc;
                }

                if (perc > PreviousPercentageDisconnect & perc > 0 & Math.Ceiling(perc) % 20 == 0)
                {
                    //StopTusdServer();
                    //StartTusdServer();
                    
                    PreviousPercentageDisconnect = Math.Ceiling(perc);
                }
            };

            var fileURL = tc.Create(ServerURL, testfile);
            tc.Upload(fileURL, testfile);

            //VerifyUpload(fileURL, testfile);

            // Cleanup
            //System.IO.File.Delete(testfile.FullName);
        }



        public static string HumanizeBytes(long bytes)
        {
            decimal res;
            res = bytes;
            if (res < 1024)
                return string.Format("{0:n2} b", res);

            res = (decimal)((double)res / (double)1024);
            if (res < 1024)
                return string.Format("{0:n2} Kb", res);

            res = (decimal)((double)res / (double)1024);
            return string.Format("{0:n2} Mb", res);
        }

        private static System.IO.FileInfo GenFileBinary(long sizeInMb)
        {
            FileInfo fi = new FileInfo(A500_MB_FILE);

            //Console.WriteLine("Generating Binary Test File...");

            //System.IO.FileInfo fi = new System.IO.FileInfo(@".\random.file");
            //if (System.IO.File.Exists(fi.FullName))
            //    System.IO.File.Delete(fi.FullName);
            //Random rnd = new Random();

            //byte[] data = new byte[sizeInMb * 1024 * 1024 - 1 + 1];
            //Random rng = new Random();
            //rng.NextBytes(data);
            //System.IO.File.WriteAllBytes(fi.FullName, data);

            //// Refresh File Info
            //fi = new System.IO.FileInfo(fi.FullName);
            return fi;
        }

        private static System.IO.FileInfo GenFileText(long sizeInMb)
        {
            FileInfo fi = new FileInfo(A500_MB_FILE);
            

            //Console.WriteLine("Generating Text Test File...");

            //System.IO.FileInfo fi = new System.IO.FileInfo(@".\random.file");
            //if (System.IO.File.Exists(fi.FullName))
            //    System.IO.File.Delete(fi.FullName);

            //var sizeInBytes = sizeInMb * 1024 * 1024;
            //long bytesWritten = 0;

            //using (System.IO.FileStream fs = new System.IO.FileStream(fi.FullName, System.IO.FileMode.Create))
            //{
            //    using (System.IO.BinaryWriter sw = new System.IO.BinaryWriter(fs))
            //    {
            //        while (bytesWritten < sizeInBytes)
            //        {
            //            var charsbytes = System.Text.Encoding.UTF8.GetBytes("A");
            //            sw.Write(charsbytes);
            //            bytesWritten += charsbytes.Length;
            //        }
            //    }
            //}

            //// Refresh File Info
            //fi = new System.IO.FileInfo(fi.FullName);

            return fi;
        }


        private static void ServerInfo()
        {
            TusClient.TusClient tc = new TusClient.TusClient();
            var serverInfo = tc.getServerInfo(ServerURL);
            Console.WriteLine("Version:{0}", serverInfo.Version);
            Console.WriteLine("Supported Protocols:{0}", serverInfo.SupportedVersions);
            Console.WriteLine("Extensions:{0}", serverInfo.Extensions);
            Console.WriteLine("MaxSize:{0}", serverInfo.MaxSize);
        }



    }
}
