import dom from "@left4code/tw-starter/dist/js/dom";

const linkTo = (menu, router, event) => {
  if (menu.subNodes) {
    menu.activeDropdown = !menu.activeDropdown;
  } else if (menu.to) {
    event.preventDefault();
    router.push(menu.to);
  }
};

const enter = (el, done) => {
  dom(el).slideDown(300);
};

const leave = (el, done) => {
  dom(el).slideUp(300);
};

export { linkTo, enter, leave };
