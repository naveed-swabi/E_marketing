using emarketing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.IO;
using PagedList;



namespace emarketing.Controllers
{
    public class AdminController : Controller
    {
        dbemarketingEntities db = new dbemarketingEntities();


        // GET: Admin
        [HttpGet]
        public ActionResult login()
        {
            return View();

        }
        [HttpPost]
        public ActionResult login(tbl_admin adm)
        {
            tbl_admin ad = db.tbl_admin.Where(m => m.ad_username == adm.ad_username && m.ad_password == adm.ad_password).SingleOrDefault();
            if (ad != null)
            {
                Session["ad_id"] = ad.ad_id;
                Session["ad-name"] = ad.ad_username;
                return RedirectToAction("create");
            }
            else
            {
                ViewBag.error = "Invalid username or password.";
            }
            return View();


        }
        public ActionResult ViewCategory(int? page)
        {
            var categories = db.tbl_category
                .Where(c => c.cat_status == 1)
                .Select(c => new
                {
                    cat_id = c.cat_id,
                    cat_name = c.cat_name,
                    cat_image = c.cat_image
                })
                .AsEnumerable()
                .Select(c => new tbl_category
                {
                    cat_id = c.cat_id,
                    cat_name = c.cat_name,
                    cat_image = c.cat_image
                }).OrderByDescending(c=>c.cat_id).ToList();

            int pageSize = 6;
            int pageNumber = (page ?? 1);

            return View(categories.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult create()
        {
            if (Session["ad_id"] == null)                
            {
               
                return RedirectToAction("login");
            }
            
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCategory(int id)
        {
            var category = db.tbl_category.Find(id);
            if (category != null)
            {
                db.tbl_category.Remove(category);
                db.SaveChanges();
            }
            return RedirectToAction("ViewCategory");
        }



        [HttpPost]
        public ActionResult create(tbl_category category, HttpPostedFileBase cat_image)
        {
            if (cat_image != null && cat_image.ContentLength > 0)
            {
                // Allowed image extensions
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".svg", ".heic", ".heif", ".ico", ".jfif", ".pjpeg", ".pjp", ".avif" }
;
                string fileExtension = Path.GetExtension(cat_image.FileName).ToLower();

                // Check if the uploaded file is an image
                if (allowedExtensions.Contains(fileExtension))
                {
                    // Ensure the "ImageUpload" folder exists in the "Content" directory
                    string uploadPath = Server.MapPath("~/Content/ImageUpload");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Generate a name for the image (without a unique identifier)
                    string imageName = Path.GetFileName(cat_image.FileName);
                    string imagePath = Path.Combine(uploadPath, imageName);

                    // Check if the image already exists
                    if (System.IO.File.Exists(imagePath))
                    {
                        ViewBag.error = "An image with the same name already exists. Please choose a different image or rename your file.";
                        return View(category);
                    }

                    // Save the image to the specified path
                    cat_image.SaveAs(imagePath);

                    // Save the image path in the database
                    category.cat_image = "~/Content/ImageUpload/" + imageName;
                    category.cat_fk_ad = Convert.ToInt32(Session["ad_id"].ToString());
                    category.cat_status = 1;

                    // Insert the category data into the database
                    db.tbl_category.Add(category);
                    db.SaveChanges();

                    return RedirectToAction("ViewCategory"); // Redirect to a category list page or another view
                }
                else
                {
                    ViewBag.error = "Only image files (jpg, jpeg, png, gif, bmp, tiff) are allowed.";
                    return View(category);
                }
            }
            else
            {
                ViewBag.error = "Please select an image file.";
                return View(category);
            }
        }
        public ActionResult Logout()
        {
            Session.Clear(); // Clear all session data
            return RedirectToAction("Usercategory"); // Redirect to Usercategory page
        }
    }
}