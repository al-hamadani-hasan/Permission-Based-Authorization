# Permission-Based-Authorization
Setting up permissions for accessing your resources is a key part of keeping your application secure. In this article, we'll go over how to use Permission-Based Authorization in ASP.NET Core, which builds on Claim-Based Authorization. We'll start from scratch to give you a clear understanding of how it all works and how it can help secure your projects.

**What’s Role-Based Authorization?**
Role-Based Authorization in ASP.NET Core helps control which users can access specific resources in your application. By using the **[Authorize]** attribute in your controller or action methods, you can restrict access based on user roles. For example, the Delete method might only be available to users assigned the 'SuperAdmin' role.

**Limitations of Role-Based Authorization**
As we add more entities to our Store Management Application — like Customers, Orders, Suppliers, and Companies—we'll also need specific admins for each department, such as CustomerAdmins and OrderAdmins. As the number of roles increases, managing them can become challenging.
Now, think about needing to change a role's permissions without changing the code. That’s tough with Role-Based Authorization. What if you wanted to add or remove a role? This could be problematic if you’ve hardcoded roles in your controllers.
If you want to create a user interface for managing users and roles, those are some significant limitations of Role-Based Authorization. 
We need a more flexible solution to manage roles and permissions dynamically, allowing a super admin to adjust them easily. This is where Permission-Based Authorization in ASP.NET Core becomes useful.

**Permission-Based Authorization in ASP.NET Core**
Let’s adopt a more flexible approach than Role-Based Authorization by introducing permissions at the role level. This way, only specific roles can access protected resources. We’ll use **RoleClaims** to manage these permissions, and ASP.NET Core provides some great features to implement this easily.

**Here are a few advantages of this approach:**
- **Dynamic Roles**: Roles aren’t hardcoded; they can be easily added, modified, or deleted at runtime.
- **Flexible Permissions**: You can adjust the permissions for each role without changing the code.
- **Greater Control**: This method gives you more control over authorization.

In this implementation, we will create a system that allows dynamic management of roles and permissions, providing a more robust and adaptable authorization framework.

**To run application with out an issue you must have**
- .NET 8 & Visual Studio [Updated version]
- SQL Server 2022 or later.
