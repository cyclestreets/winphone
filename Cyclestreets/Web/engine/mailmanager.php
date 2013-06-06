<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/index.php $
 * $Revision: 8 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-02-05 20:19:13 +0000 (Tue, 05 Feb 2008) $
 *********************************************************/
 
class mailmanager
{
	function mailmanager()
	{
	}

    function AddEmailToQueue($subject, $msg, $sender, $recipients=array())
    {
        global $DB, $std;

        if ( empty($recipients) )
            return;

        $msg = addslashes($msg);

        $query = "INSERT INTO `dl_mailqueue` (`date`, `to`, `from`,  `subject`, `content` ) VALUES \n";
        foreach ( $recipients as $rec )
        {
            if ( $std->emailvalidate($rec) )
            {
                $query .= "( '".time()."', '{$rec}', '{$sender}', '{$subject}', '{$msg}' )\n";
            }
            else
            {
                $this->logError("{$rec} is not a valid email address!");
            }
        }
        $DB->query($query);
    }

    function logError($err)
    {
        //TODO!
    }
}

?>