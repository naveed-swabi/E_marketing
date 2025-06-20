using emarketing.Models;
using Microsoft.Ajax.Utilities;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace emarketing.Controllers
{
    public class UserController : Controller
    {
        dbemarketingEntities db = new dbemarketingEntities();


        // GET: User
        public ActionResult Usercategory(int? page)
        {
            var usertable = db.tbl_user;
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
                }).OrderByDescending(c => c.cat_id).ToList();

            int pageSize = 6;
            int pageNumber = (page ?? 1);

            return View(categories.ToPagedList(pageNumber, pageSize));
        }
        [HttpGet]
        public ActionResult login()
        {
            return View();

        }
        [HttpPost]
        public ActionResult login(tbl_user user)
        {
            tbl_user u = db.tbl_user.Where(m => m.u_email == user.u_email && m.u_password == user.u_password).SingleOrDefault();
            if (u != null)
            {
                Session["u_id"] = u.u_id;
                Session["username"] = u.u_name;
                return RedirectToAction("Usercategory");
            }
            else
            {
                ViewBag.error = "Invalid email or password.";
            }
            return View();


        }
        [HttpGet]
        public ActionResult Signup()
        {
            return View();

        }

        [HttpPost]
        public ActionResult Signup(tbl_user usertable, HttpPostedFileBase imageFile)
        {
            // Check if the email already exists in the database
            var existingUserByEmail = db.tbl_user.FirstOrDefault(u => u.u_email == usertable.u_email);
            if (existingUserByEmail != null)
            {
                ViewBag.error = "The email is already used. Please try another email.";
                return View(usertable);
            }

            // Check if the contact number already exists in the database
            var existingUserByContact = db.tbl_user.FirstOrDefault(u => u.u_contact == usertable.u_contact);
            if (existingUserByContact != null)
            {
                ViewBag.error = "The contact number is already used. Please try another contact number.";
                return View(usertable);
            }

            // Proceed with image file handling
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                // Allowed image extensions
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
                string fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                // Check if the uploaded file is an image
                if (allowedExtensions.Contains(fileExtension))
                {
                    // Ensure the "ImageUpload" folder exists
                    string uploadPath = Server.MapPath("~/Content/ImageUpload");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Generate a unique name for the image to avoid conflicts
                    string imageName = Path.GetFileNameWithoutExtension(imageFile.FileName)
                                       + "_" + Guid.NewGuid().ToString()
                                       + fileExtension;
                    string imagePath = Path.Combine(uploadPath, imageName);

                    // Save the image to the specified path
                    imageFile.SaveAs(imagePath);

                    // Create a new user object
                    var user = new tbl_user
                    {
                        u_email = usertable.u_email,
                        u_password = usertable.u_password,
                        u_name = usertable.u_name,
                        u_contact = usertable.u_contact,
                        u_image = "~/Content/ImageUpload/" + imageName
                    };

                    // Insert the new user data into the database
                    db.tbl_user.Add(user);
                    db.SaveChanges();

                    return RedirectToAction("login"); // Redirect to a login page or another view
                }
                else
                {
                    ViewBag.error = "Only image files (jpg, jpeg, png, gif, bmp, tiff) are allowed.";
                    return View(usertable);
                }
            }
            else
            {
                ViewBag.error = "Please select an image file.";
                return View(usertable);
            }
        }
        [HttpGet]
        public ActionResult CreateAd()
        {
            // Fetch categories from the database
            var categories = db.tbl_category.ToList();
            ViewBag.Categories = categories;

            return View();
        }

        [HttpPost]
        public ActionResult CreateAd(tbl_product model, HttpPostedFileBase ProductImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Fetch the current user
                    var currentUser = Session["u_id"];

                    if (currentUser == null)
                    {
                        // Handle the case where the user is not logged in
                        return RedirectToAction("Login");
                    }

                    // Validate the file type
                    if (ProductImage != null && ProductImage.ContentLength > 0)
                    {
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".svg", ".heic", ".heif", ".ico", ".jfif", ".pjpeg", ".pjp", ".avif" }
;
                        string extension = Path.GetExtension(ProductImage.FileName).ToLower();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("ProductImage", "Only image files are allowed.");
                        }
                        else
                        {
                            // Save the file
                            string fileName = Guid.NewGuid().ToString() + extension;
                            string path = Path.Combine(Server.MapPath("~/Content/ImageUpload"), fileName);
                            ProductImage.SaveAs(path);

                            // Create the product entity
                            var product = new tbl_product
                            {
                                pro_name = model.pro_name,
                                pro_image = "~/Content/ImageUpload/" + fileName,
                                pro_des = model.pro_des,
                                pro_pric = model.pro_pric,
                                pro_fk_cat = model.pro_fk_cat,
                                pro_fk_user = Convert.ToInt32(currentUser)
                            };

                            // Add and save to the database
                            db.tbl_product.Add(product);
                            db.SaveChanges();

                            return RedirectToAction("Usercategory"); // Redirect to a suitable action after successful save
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ProductImage", "Please upload an image file.");
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
            }

            // If model is not valid, return the same view with the current data
            var categories = db.tbl_category.ToList();
            ViewBag.Categories = categories;
            return View(model);
        }

        public ActionResult Logout()
        {
            Session.Clear(); // Clear all session data
            return RedirectToAction("Usercategory"); // Redirect to Usercategory page
        }
        public ActionResult Ad(int? id, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Fetch the products based on the selected category id
            var products = db.tbl_product
                .Where(pr => pr.pro_fk_cat == id)
                .OrderByDescending(p => p.pro_id)
                .ToList();

            // If no products are found, return to a relevant view or show a message
            if (!products.Any())
            {
                ViewBag.Message = "No products found for this category.";
                return View("Usercategory"); // Return a view that handles no products scenario
            }

            int pageSize = 9;
            int pageNumber = (page ?? 1);

            return View(products.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult ViewAd(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Ensure tbl_user is a navigation property in tbl_product
            var product = db.tbl_product
                            .Include("tbl_user") // Include related tbl_user data using a string
                            .FirstOrDefault(pr => pr.pro_id == id);

            if (product == null)
            {
                return HttpNotFound("Product not found.");
            }

            return View(product);
        }


        public ActionResult DeletAd(int? id)
        {
            if (id == null)
            {
                // Handle the case where id is null if necessary
                TempData["ErrorMessage"] = "Invalid product ID.";
                return RedirectToAction("Usercategory");
            }

            // Find the product by ID
            tbl_product p = db.tbl_product.SingleOrDefault(x => x.pro_id == id);

            if (p == null)
            {
                // If the product was not found, it may have been deleted or does not exist
                TempData["ErrorMessage"] = "The product you are trying to delete does not exist.";
                return RedirectToAction("Usercategory");
            }

            // Remove the product
            db.tbl_product.Remove(p);
            db.SaveChanges();

            // Set a success message (optional)
            TempData["SuccessMessage"] = "The product has been deleted successfully.";

            // Redirect to Usercategory to refresh the list
            return RedirectToAction("Usercategory");
        }
        [HttpGet]
        public ActionResult Search(string query, int? page)
        {
            // Redirect to Usercategory view if no search term is provided
            if (string.IsNullOrEmpty(query))
            {
                return RedirectToAction("Usercategory");
            }

            // Perform the search operation
            var products = db.tbl_product
                .Where(p => p.pro_name.Contains(query) || p.pro_des.Contains(query))
                .OrderByDescending(p => p.pro_id)
                .ToList();

            // Check if any products are found
            if (!products.Any())
            {
                ViewBag.Message = "No products found.";
            }

            // Set up pagination
            int pageSize = 6; // Number of items per page
            int pageNumber = (page ?? 1); // Current page number, default to 1

            // Return the view with paginated products
            return View("Ad", products.ToPagedList(pageNumber, pageSize));
        }


    }

}

