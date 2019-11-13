import { defineMessages } from 'react-intl'

/* eslint-disable max-len */
export default defineMessages({
  advanced_page_title: 'Advanced',

  generalinfo_pane_title: 'General Info',
  nodeinfo_pane_title: 'Network & Connections',
  stakinginfo_pane_title: 'Staking Info',
  importkeys_pane_title: 'Import Keys',
  exportkeys_pane_title: 'Export Keys',
  rescan_pane_title: 'Rescan Blocks',

  backup_method: 'Backup Method',
  backup_method_dropbox: 'Dropbox',
  backup_method_gdrive: 'Google Drive',
  backup_method_local: 'Backup to local file',
  connect_pane_title: 'Staking Info',
  copy_lndconnect: 'Copy Lndconnect',
  copy_pubkey: 'Copy Pubkey',
  export_keys_copy_notification: 'The exported keys were copied to the clipboard.',
  lndconnect_description: 'This QR Code can be used to connect other devices to your lnd node. It can be used with Zap iOS and Zap Android and any other wallet that supports the lndconnect standard.',
  lndconnect_hide_button: 'Click to hide QR Code',
  lndconnect_reveal_button: 'Click to reveal QR Code',
  lndconnect_title: 'LndConnect QR Code',
  lndconnect_warning: 'Keep this private! If someone gains access to this QR Code they can steal your money.',
  node_pubkey_description: 'Node pubkey (short for public key) is the public address of your node. This is your unique identifier, generated from your seed.',
  node_pubkey_title: 'Node Pubkey',
  node_version: 'Node Version',
  
  
  pubkey_copied_notification_description: 'Node public key has been copied to your clipboard',
})
