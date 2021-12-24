USE [master]--注意不能够USE [TestDB]，因为[TestDB]即将被删除，所以不能够将当前连接设置为连接到[TestDB]，否则下面的DROP DATABASE语句会报错

ALTER DATABASE [NCF-Dapr] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;--首先将数据库改为单用户模式，WITH ROLLBACK IMMEDIATE提示切断所有其它连接到[TestDB]的数据库连接
DROP DATABASE [NCF-Dapr];--删除[TestDB]及其数据库文件


